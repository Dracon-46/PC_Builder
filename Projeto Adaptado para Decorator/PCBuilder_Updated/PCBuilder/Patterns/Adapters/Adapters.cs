using PCBuilder.Models;
using PCBuilder.ViewModels;

namespace PCBuilder.Patterns.Adapters;

// ═══════════════════════════════════════════════════════════════════════════════
// ADAPTER PATTERN
//
// Propósito: converter uma interface incompatível em outra que o cliente espera.
// Aqui temos três adaptadores concretos, cada um resolvendo um problema real:
//
//  1. ICurrencyAdapter  — adapta preços BRL para USD / EUR via API real (AwesomeAPI)
//  2. IComponentSpecAdapter — adapta Product interno para DTO de fornecedor externo
//  3. INotificationAdapter — adapta o envio de notificações para múltiplos canais
// ═══════════════════════════════════════════════════════════════════════════════


// ── 1. CURRENCY ADAPTER ──────────────────────────────────────────────────────
// Target interface (o que o sistema espera receber)
public interface ICurrencyAdapter
{
    string CurrencyCode { get; }
    string CurrencySymbol { get; }
    decimal Convert(decimal brlAmount);
    string Format(decimal brlAmount);
}

// ── ExchangeRateProvider — busca cotações reais via AwesomeAPI (gratuita, sem chave) ──
// Em caso de falha de rede, utiliza taxas fallback para não quebrar a aplicação.
public sealed class ExchangeRateProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExchangeRateProvider> _logger;

    // Cache em memória: evita chamadas repetidas à API
    private decimal _usdRate   = 0m;
    private decimal _eurRate   = 0m;
    private DateTime _lastFetch = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);
    private readonly SemaphoreSlim _lock = new(1, 1);

    // Taxas fallback usadas somente se a API estiver inacessível
    private const decimal FallbackUsd = 0.19m;
    private const decimal FallbackEur = 0.175m;

    public ExchangeRateProvider(IHttpClientFactory httpClientFactory, ILogger<ExchangeRateProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<decimal> GetUsdRateAsync()
    {
        await EnsureFreshAsync();
        return _usdRate > 0 ? _usdRate : FallbackUsd;
    }

    public async Task<decimal> GetEurRateAsync()
    {
        await EnsureFreshAsync();
        return _eurRate > 0 ? _eurRate : FallbackEur;
    }

    public DateTime LastUpdated => _lastFetch == DateTime.MinValue ? DateTime.UtcNow.Date : _lastFetch;

    private async Task EnsureFreshAsync()
    {
        if (DateTime.UtcNow - _lastFetch < _cacheDuration) return;

        await _lock.WaitAsync();
        try
        {
            // double-check após adquirir lock
            if (DateTime.UtcNow - _lastFetch < _cacheDuration) return;

            // AwesomeAPI: https://docs.awesomeapi.com.br/api-de-moedas
            // Endpoint: /json/last/USD-BRL,EUR-BRL
            var client = _httpClientFactory.CreateClient("AwesomeApi");
            var response = await client.GetFromJsonAsync<AwesomeApiResponse>(
                "https://economia.awesomeapi.com.br/json/last/USD-BRL,EUR-BRL");

            if (response?.USDBRL?.Bid is not null && decimal.TryParse(
                    response.USDBRL.Bid,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var usdBrl) && usdBrl > 0)
            {
                // API retorna BRL por 1 USD → invertemos para obter USD por 1 BRL
                _usdRate = Math.Round(1m / usdBrl, 6);
            }

            if (response?.EURBRL?.Bid is not null && decimal.TryParse(
                    response.EURBRL.Bid,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var eurBrl) && eurBrl > 0)
            {
                _eurRate = Math.Round(1m / eurBrl, 6);
            }

            _lastFetch = DateTime.UtcNow;
            _logger.LogInformation(
                "[ExchangeRateProvider] Cotações atualizadas: USD={Usd}, EUR={Eur} (fonte: AwesomeAPI)",
                _usdRate, _eurRate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ExchangeRateProvider] Falha ao buscar cotações. Usando taxas fallback.");
            // Mantém _usdRate / _eurRate como 0 → GetUsdRateAsync/GetEurRateAsync retornam fallback
        }
        finally
        {
            _lock.Release();
        }
    }

    // DTOs para desserializar a resposta da AwesomeAPI
    private sealed class AwesomeApiResponse
    {
        public AwesomeCurrency? USDBRL { get; set; }
        public AwesomeCurrency? EURBRL { get; set; }
    }

    private sealed class AwesomeCurrency
    {
        public string? Bid { get; set; }   // taxa de venda (usamos como referência)
        public string? Ask { get; set; }
        public string? Name { get; set; }
    }
}

// Adapter concreto: BRL → USD (usa taxa real da API)
public sealed class UsdCurrencyAdapter : ICurrencyAdapter
{
    private readonly ExchangeRateProvider _provider;
    public UsdCurrencyAdapter(ExchangeRateProvider provider) => _provider = provider;

    public string CurrencyCode   => "USD";
    public string CurrencySymbol => "$";

    public decimal Convert(decimal brlAmount)
    {
        var rate = _provider.GetUsdRateAsync().GetAwaiter().GetResult();
        return Math.Round(brlAmount * rate, 2);
    }

    public string Format(decimal brlAmount) => $"$ {Convert(brlAmount):N2}";
}

// Adapter concreto: BRL → EUR (usa taxa real da API)
public sealed class EurCurrencyAdapter : ICurrencyAdapter
{
    private readonly ExchangeRateProvider _provider;
    public EurCurrencyAdapter(ExchangeRateProvider provider) => _provider = provider;

    public string CurrencyCode   => "EUR";
    public string CurrencySymbol => "€";

    public decimal Convert(decimal brlAmount)
    {
        var rate = _provider.GetEurRateAsync().GetAwaiter().GetResult();
        return Math.Round(brlAmount * rate, 2);
    }

    public string Format(decimal brlAmount) => $"€ {Convert(brlAmount):N2}";
}

// Seletor de adapter por código
public interface ICurrencyAdapterFactory
{
    ICurrencyAdapter GetAdapter(string currencyCode);
    IEnumerable<string> SupportedCurrencies { get; }
}

public sealed class CurrencyAdapterFactory : ICurrencyAdapterFactory
{
    private readonly Dictionary<string, ICurrencyAdapter> _adapters;

    public CurrencyAdapterFactory(ExchangeRateProvider provider)
    {
        _adapters = new Dictionary<string, ICurrencyAdapter>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = new UsdCurrencyAdapter(provider),
            ["EUR"] = new EurCurrencyAdapter(provider),
        };
    }

    public ICurrencyAdapter GetAdapter(string currencyCode) =>
        _adapters.TryGetValue(currencyCode, out var a) ? a : _adapters["USD"];

    public IEnumerable<string> SupportedCurrencies => _adapters.Keys;
}


// ── 2. COMPONENT SPEC ADAPTER ─────────────────────────────────────────────────
public sealed class ExternalSupplierComponentDto
{
    public string SupplierSku       { get; set; } = string.Empty;
    public string FullTitle         { get; set; } = string.Empty;
    public string Manufacturer      { get; set; } = string.Empty;
    public double PriceUsd          { get; set; }
    public int    TdpWatts          { get; set; }
    public string SocketType        { get; set; } = string.Empty;
    public int    WarrantyMonths    { get; set; }
    public string AvailabilityCode  { get; set; } = "IN_STOCK";
    public string CategoryCode      { get; set; } = string.Empty;
}

public interface IComponentSpecAdapter
{
    ExternalSupplierComponentDto ToExternalDto(Product product);
    Product FromExternalDto(ExternalSupplierComponentDto dto);
    EnrichedProductViewModel Enrich(ProductViewModel vm, ExternalSupplierComponentDto dto);
}

public sealed class ExternalSupplierAdapter : IComponentSpecAdapter
{
    private static readonly Dictionary<string, ComponentType> _categoryMap = new()
    {
        ["CPU"]     = ComponentType.CPU,
        ["GPU"]     = ComponentType.GPU,
        ["RAM"]     = ComponentType.RAM,
        ["STORAGE"] = ComponentType.Storage,
        ["MB"]      = ComponentType.Motherboard,
        ["PSU"]     = ComponentType.PowerSupply,
        ["COOLER"]  = ComponentType.Cooler,
    };

    private static readonly Dictionary<ComponentType, string> _reverseCategoryMap = new()
    {
        [ComponentType.CPU]          = "CPU",
        [ComponentType.GPU]          = "GPU",
        [ComponentType.RAM]          = "RAM",
        [ComponentType.Storage]      = "STORAGE",
        [ComponentType.Motherboard]  = "MB",
        [ComponentType.PowerSupply]  = "PSU",
        [ComponentType.Cooler]       = "COOLER",
    };

    public ExternalSupplierComponentDto ToExternalDto(Product product) => new()
    {
        SupplierSku      = $"PCB-{product.Id:D6}",
        FullTitle        = $"{product.Brand} {product.Name}",
        Manufacturer     = product.Brand,
        PriceUsd         = (double)(product.Price * 0.19m),
        TdpWatts         = product.TDP ?? product.PowerConsumption,
        SocketType       = product.Socket ?? "N/A",
        WarrantyMonths   = 24,
        AvailabilityCode = product.IsAvailable ? "IN_STOCK" : "OUT_OF_STOCK",
        CategoryCode     = _reverseCategoryMap.TryGetValue(product.Type, out var c) ? c : "UNKNOWN",
    };

    public Product FromExternalDto(ExternalSupplierComponentDto dto) => new()
    {
        Name             = dto.FullTitle.Replace(dto.Manufacturer, "").Trim(),
        Brand            = dto.Manufacturer,
        Price            = (decimal)(dto.PriceUsd / 0.19),
        PowerConsumption = dto.TdpWatts,
        Socket           = dto.SocketType == "N/A" ? null : dto.SocketType,
        IsAvailable      = dto.AvailabilityCode == "IN_STOCK",
        Type             = _categoryMap.TryGetValue(dto.CategoryCode, out var t) ? t : ComponentType.CPU,
        Description      = $"Importado via fornecedor externo. Garantia: {dto.WarrantyMonths} meses.",
    };

    public EnrichedProductViewModel Enrich(ProductViewModel vm, ExternalSupplierComponentDto dto) => new()
    {
        Id              = vm.Id,
        Name            = vm.Name,
        Brand           = vm.Brand,
        Description     = vm.Description,
        Price           = vm.Price,
        Type            = vm.Type,
        PowerConsumption= vm.PowerConsumption,
        Socket          = vm.Socket,
        TDP             = vm.TDP,
        WattageCapacity = vm.WattageCapacity,
        SupplierSku     = dto.SupplierSku,
        WarrantyMonths  = dto.WarrantyMonths,
        AvailabilityStatus = dto.AvailabilityCode switch
        {
            "IN_STOCK"    => "Em estoque",
            "LOW_STOCK"   => "Últimas unidades",
            "OUT_OF_STOCK"=> "Indisponível",
            _             => dto.AvailabilityCode
        },
        AvailabilityCode = dto.AvailabilityCode,
    };
}


// ── 3. NOTIFICATION ADAPTER ───────────────────────────────────────────────────
public sealed class NotificationMessage
{
    public string To      { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body    { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
}

public interface INotificationAdapter
{
    string ChannelName { get; }
    Task SendAsync(NotificationMessage message);
}

public sealed class SmtpEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    public SmtpEmailService(ILogger<SmtpEmailService> logger) => _logger = logger;

    public Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        _logger.LogInformation("[SMTP] Para: {To} | Assunto: {Subject}", to, subject);
        _logger.LogInformation("[SMTP] Corpo: {Body}", htmlBody);
        return Task.CompletedTask;
    }
}

public sealed class EmailNotificationAdapter : INotificationAdapter
{
    private readonly SmtpEmailService _smtp;
    public EmailNotificationAdapter(SmtpEmailService smtp) => _smtp = smtp;

    public string ChannelName => "Email";
    public Task SendAsync(NotificationMessage message) =>
        _smtp.SendEmailAsync(message.To, message.Subject, $"<p>{message.Body}</p>");
}

public sealed class FileLogService
{
    private readonly ILogger<FileLogService> _logger;
    public FileLogService(ILogger<FileLogService> logger) => _logger = logger;

    public Task WriteAsync(string entry)
    {
        _logger.LogInformation("[FILE-LOG] {Entry}", entry);
        return Task.CompletedTask;
    }
}

public sealed class LogFileNotificationAdapter : INotificationAdapter
{
    private readonly FileLogService _fileLog;
    public LogFileNotificationAdapter(FileLogService fileLog) => _fileLog = fileLog;

    public string ChannelName => "Log";
    public Task SendAsync(NotificationMessage message) =>
        _fileLog.WriteAsync(
            $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] TO={message.To} | {message.Subject} | {message.Body}");
}

public interface INotificationDispatcher
{
    Task NotifyOrderPlacedAsync(string customerName, string customerEmail, string orderNumber, decimal total);
}

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationAdapter> _adapters;
    public NotificationDispatcher(IEnumerable<INotificationAdapter> adapters) => _adapters = adapters;

    public async Task NotifyOrderPlacedAsync(
        string customerName, string customerEmail, string orderNumber, decimal total)
    {
        var message = new NotificationMessage
        {
            To      = customerEmail,
            Subject = $"Pedido {orderNumber} confirmado — PCBuilder",
            Body    = $"Olá {customerName}! Seu pedido {orderNumber} no valor de R$ {total:N2} foi confirmado com sucesso.",
            Channel = "all"
        };
        foreach (var adapter in _adapters)
            await adapter.SendAsync(message);
    }
}
