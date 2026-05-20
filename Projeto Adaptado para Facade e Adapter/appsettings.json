using PCBuilder.Models;
using PCBuilder.ViewModels;

namespace PCBuilder.Patterns.Adapters;

// ═══════════════════════════════════════════════════════════════════════════════
// ADAPTER PATTERN
//
// Propósito: converter uma interface incompatível em outra que o cliente espera.
// Aqui temos três adaptadores concretos, cada um resolvendo um problema real:
//
//  1. ICurrencyAdapter  — adapta preços BRL para USD / EUR
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

// Adaptee simulado: seria uma API externa de câmbio (ex: OpenExchangeRates)
// Aqui usamos taxas fixas para fins de portfólio
public sealed class ExchangeRateProvider
{
    // Taxas fixas simulando uma API externa — em produção viriam de HTTP call
    public decimal GetUsdRate() => 0.19m;   // 1 BRL ≈ 0.19 USD
    public decimal GetEurRate() => 0.175m;  // 1 BRL ≈ 0.175 EUR
    public DateTime LastUpdated => DateTime.UtcNow.Date;
}

// Adapter concreto: BRL → USD
public sealed class UsdCurrencyAdapter : ICurrencyAdapter
{
    private readonly ExchangeRateProvider _provider;
    public UsdCurrencyAdapter(ExchangeRateProvider provider) => _provider = provider;

    public string CurrencyCode   => "USD";
    public string CurrencySymbol => "$";

    public decimal Convert(decimal brlAmount) =>
        Math.Round(brlAmount * _provider.GetUsdRate(), 2);

    public string Format(decimal brlAmount) =>
        $"$ {Convert(brlAmount):N2}";
}

// Adapter concreto: BRL → EUR
public sealed class EurCurrencyAdapter : ICurrencyAdapter
{
    private readonly ExchangeRateProvider _provider;
    public EurCurrencyAdapter(ExchangeRateProvider provider) => _provider = provider;

    public string CurrencyCode   => "EUR";
    public string CurrencySymbol => "€";

    public decimal Convert(decimal brlAmount) =>
        Math.Round(brlAmount * _provider.GetEurRate(), 2);

    public string Format(decimal brlAmount) =>
        $"€ {Convert(brlAmount):N2}";
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
// Simulação: um fornecedor externo retorna componentes num formato diferente.
// O Adapter converte entre o modelo interno (Product) e o DTO externo.

// Formato do "fornecedor externo" (Adaptee) — não podemos modificar isso
public sealed class ExternalSupplierComponentDto
{
    public string SupplierSku       { get; set; } = string.Empty;
    public string FullTitle         { get; set; } = string.Empty;
    public string Manufacturer      { get; set; } = string.Empty;
    public double PriceUsd          { get; set; }
    public int    TdpWatts          { get; set; }
    public string SocketType        { get; set; } = string.Empty;
    public int    WarrantyMonths    { get; set; }
    public string AvailabilityCode  { get; set; } = "IN_STOCK"; // IN_STOCK | LOW_STOCK | OUT_OF_STOCK
    public string CategoryCode      { get; set; } = string.Empty; // CPU | GPU | RAM | STORAGE | MB | PSU | COOLER
}

// Target interface — o que o sistema interno espera
public interface IComponentSpecAdapter
{
    /// <summary>Converte um Product interno para o DTO do fornecedor externo.</summary>
    ExternalSupplierComponentDto ToExternalDto(Product product);

    /// <summary>Converte um DTO externo de volta para um Product interno.</summary>
    Product FromExternalDto(ExternalSupplierComponentDto dto);

    /// <summary>Enriquece um ProductViewModel com dados do fornecedor.</summary>
    EnrichedProductViewModel Enrich(ProductViewModel vm, ExternalSupplierComponentDto dto);
}

// Adapter concreto
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

    private static readonly Dictionary<ComponentType, string> _reverseCategoryMap =
        new Dictionary<ComponentType, string>
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
        WarrantyMonths   = 24, // padrão de mercado
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
        // campos originais
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
        // campos enriquecidos via adapter
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
// Propósito: desacoplar o envio de notificações do canal concreto.
// O sistema só conhece INotificationAdapter — não sabe se é e-mail, log, SMS, etc.

public sealed class NotificationMessage
{
    public string To      { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body    { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty; // Email | Console | Log
}

// Target interface
public interface INotificationAdapter
{
    string ChannelName { get; }
    Task SendAsync(NotificationMessage message);
}

// Adaptee 1: sistema de e-mail externo simulado (ex: SendGrid, MailKit)
// Em produção teria SmtpClient ou HttpClient para API de e-mail
public sealed class SmtpEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    public SmtpEmailService(ILogger<SmtpEmailService> logger) => _logger = logger;

    // Simula envio SMTP — em produção seria SmtpClient.SendMailAsync()
    public Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        _logger.LogInformation("[SMTP] Para: {To} | Assunto: {Subject}", to, subject);
        _logger.LogInformation("[SMTP] Corpo: {Body}", htmlBody);
        return Task.CompletedTask;
    }
}

// Adapter: adapta SmtpEmailService → INotificationAdapter
public sealed class EmailNotificationAdapter : INotificationAdapter
{
    private readonly SmtpEmailService _smtp;
    public EmailNotificationAdapter(SmtpEmailService smtp) => _smtp = smtp;

    public string ChannelName => "Email";

    public Task SendAsync(NotificationMessage message) =>
        _smtp.SendEmailAsync(message.To, message.Subject, $"<p>{message.Body}</p>");
}

// Adaptee 2: sistema de log em arquivo
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

// Adapter: adapta FileLogService → INotificationAdapter
public sealed class LogFileNotificationAdapter : INotificationAdapter
{
    private readonly FileLogService _fileLog;
    public LogFileNotificationAdapter(FileLogService fileLog) => _fileLog = fileLog;

    public string ChannelName => "Log";

    public Task SendAsync(NotificationMessage message) =>
        _fileLog.WriteAsync(
            $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] TO={message.To} | {message.Subject} | {message.Body}");
}

// Dispatcher: envia para todos os adapters registrados
public interface INotificationDispatcher
{
    Task NotifyOrderPlacedAsync(string customerName, string customerEmail, string orderNumber, decimal total);
}

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationAdapter> _adapters;

    public NotificationDispatcher(IEnumerable<INotificationAdapter> adapters) =>
        _adapters = adapters;

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
