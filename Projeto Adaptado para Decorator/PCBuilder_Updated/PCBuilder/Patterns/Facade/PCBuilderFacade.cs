using PCBuilder.Models;
using PCBuilder.Patterns.Adapters;
using PCBuilder.Patterns.Decorators;
using PCBuilder.Repositories;
using PCBuilder.Services;
using PCBuilder.ViewModels;

namespace PCBuilder.Patterns.Facade;

// ═══════════════════════════════════════════════════════════════════════════════
// FACADE PATTERN
//
// Propósito: fornecer uma interface simplificada para um subsistema complexo.
// Agora também orquestra o DECORATOR de descontos e a API real de câmbio.
// ═══════════════════════════════════════════════════════════════════════════════

public interface IPCBuilderFacade
{
    // ── Catálogo ──────────────────────────────────────────────────────────────
    Task<IEnumerable<BuildViewModel>> GetAllTemplatesAsync();
    Task<IEnumerable<BuildViewModel>> GetTemplatesByCategoryAsync(BuildCategory category);
    Task<BuildViewModel?> GetBuildViewModelAsync(int buildId);
    Task<BuildDetailViewModel?> GetBuildDetailAsync(int buildId, string currencyCode = "BRL");

    // ── Personalização ────────────────────────────────────────────────────────
    Task<CustomizeViewModel> GetCustomizeViewModelAsync(int? sourceBuildId, string? sessionId);
    Task<BuildViewModel?> GetCustomBuildBySessionAsync(string sessionId);

    Task<SaveCustomizationResult> SaveCustomizationAsync(
        string sessionId,
        int? cpuId, int? gpuId, int? ramId, int? storageId,
        int? motherboardId, int? psuId, int? coolerId);

    Task<Build> CloneAsCustomAsync(int templateBuildId, string sessionId);

    // ── Compatibilidade e Preço (AJAX) ────────────────────────────────────────
    Task<LivePriceResult> GetLivePriceAsync(
        int? cpuId, int? gpuId, int? ramId, int? storageId,
        int? motherboardId, int? psuId, int? coolerId,
        string currencyCode = "BRL");

    // ── Pedido ────────────────────────────────────────────────────────────────
    Task<OrderSummaryViewModel> GetOrderSummaryAsync(int? buildId, string? sessionId, string? couponCode = null);
    Task<Order> PlaceOrderAsync(PlaceOrderRequest request);
    Task<OrderConfirmationViewModel?> GetOrderConfirmationAsync(string orderNumber);

    // ── Componentes enriquecidos (Adapter) ────────────────────────────────────
    Task<IEnumerable<EnrichedProductViewModel>> GetEnrichedProductsAsync(ComponentType type);

    // ── Moedas disponíveis ────────────────────────────────────────────────────
    IEnumerable<string> GetSupportedCurrencies();

    // ── Decorator: desconto ───────────────────────────────────────────────────
    PriceBreakdownViewModel GetPriceBreakdown(
        decimal basePrice,
        BuildCategory? category = null,
        string? couponCode = null,
        int componentCount = 0);

    bool ValidateCoupon(string? code);
}

// ── DTOs de resultado ────────────────────────────────────────────────────────

public sealed class SaveCustomizationResult
{
    public bool IsValid { get; init; }
    public List<CompatibilityError> Errors { get; init; } = [];
    public string? SessionId { get; init; }
    public decimal EstimatedPrice { get; init; }
    public int EstimatedPower { get; init; }
}

public sealed class LivePriceResult
{
    public string TotalFormatted    { get; init; } = string.Empty;
    public decimal TotalRaw         { get; init; }
    public string? TotalConverted   { get; init; }
    public string? CurrencyCode     { get; init; }
    public int Power                { get; init; }
    public List<CompatibilityErrorDto> Errors { get; init; } = [];

    // Decorator: breakdown de descontos em tempo real
    public PriceBreakdownViewModel? PriceBreakdown { get; init; }
}

public sealed class CompatibilityErrorDto
{
    public string Component { get; init; } = string.Empty;
    public string Message   { get; init; } = string.Empty;
    public string Severity  { get; init; } = string.Empty;
}

public sealed class PlaceOrderRequest
{
    public string SessionId      { get; init; } = string.Empty;
    public int?   BuildId        { get; init; }
    public string CustomerName   { get; init; } = string.Empty;
    public string CustomerEmail  { get; init; } = string.Empty;
    public string CustomerPhone  { get; init; } = string.Empty;
    public string ShippingAddress{ get; init; } = string.Empty;
    public string? CouponCode    { get; init; }
    public BuildCategory? Category { get; init; }
}

// ── Implementação da Facade ──────────────────────────────────────────────────

public sealed class PCBuilderFacade : IPCBuilderFacade
{
    private readonly IBuildService             _buildService;
    private readonly ICompatibilityService     _compatibility;
    private readonly IPricingService           _pricing;
    private readonly IOrderService             _orderService;
    private readonly IProductRepository        _productRepo;
    private readonly IBuildRepository          _buildRepo;
    private readonly ICurrencyAdapterFactory   _currencyFactory;
    private readonly IComponentSpecAdapter     _specAdapter;
    private readonly INotificationDispatcher   _notifier;
    private readonly IDiscountService          _discountService;

    public PCBuilderFacade(
        IBuildService buildService,
        ICompatibilityService compatibility,
        IPricingService pricing,
        IOrderService orderService,
        IProductRepository productRepo,
        IBuildRepository buildRepo,
        ICurrencyAdapterFactory currencyFactory,
        IComponentSpecAdapter specAdapter,
        INotificationDispatcher notifier,
        IDiscountService discountService)
    {
        _buildService    = buildService;
        _compatibility   = compatibility;
        _pricing         = pricing;
        _orderService    = orderService;
        _productRepo     = productRepo;
        _buildRepo       = buildRepo;
        _currencyFactory = currencyFactory;
        _specAdapter     = specAdapter;
        _notifier        = notifier;
        _discountService = discountService;
    }

    // ── Catálogo ──────────────────────────────────────────────────────────────

    public Task<IEnumerable<BuildViewModel>> GetAllTemplatesAsync() =>
        _buildService.GetAllTemplatesAsync();

    public Task<IEnumerable<BuildViewModel>> GetTemplatesByCategoryAsync(BuildCategory category) =>
        _buildService.GetTemplatesByCategoryAsync(category);

    public Task<BuildViewModel?> GetBuildViewModelAsync(int buildId) =>
        _buildService.GetBuildViewModelAsync(buildId);

    public async Task<BuildDetailViewModel?> GetBuildDetailAsync(int buildId, string currencyCode = "BRL")
    {
        var build = await _buildService.GetBuildViewModelAsync(buildId);
        if (build == null) return null;

        var usdAdapter = _currencyFactory.GetAdapter("USD");
        var eurAdapter = _currencyFactory.GetAdapter("EUR");

        // Preço total convertido
        string? convertedPrice = null;
        if (!string.Equals(currencyCode, "BRL", StringComparison.OrdinalIgnoreCase))
        {
            var adapter = _currencyFactory.GetAdapter(currencyCode);
            convertedPrice = adapter.Format(build.TotalPrice);
        }

        // Enriquecer componentes via ExternalSupplierAdapter
        var enrichedComponents = new List<EnrichedProductViewModel>();
        var componentPricesUsd = new Dictionary<int, string>();
        var componentPricesEur = new Dictionary<int, string>();

        var allComponents = new[] { build.CPU, build.GPU, build.RAM, build.Storage,
                                    build.Motherboard, build.PowerSupply, build.Cooler };
        foreach (var comp in allComponents.Where(c => c != null))
        {
            var externalDto = _specAdapter.ToExternalDto(new Models.Product
            {
                Id = comp!.Id, Name = comp.Name, Brand = comp.Brand,
                Type = comp.Type, Price = comp.Price,
                PowerConsumption = comp.PowerConsumption,
                Socket = comp.Socket, TDP = comp.TDP,
                WattageCapacity = comp.WattageCapacity, IsAvailable = true
            });
            enrichedComponents.Add(_specAdapter.Enrich(comp, externalDto));

            // Converter preço de cada componente via Adapter
            componentPricesUsd[comp.Id] = usdAdapter.Format(comp.Price);
            componentPricesEur[comp.Id] = eurAdapter.Format(comp.Price);
        }

        return new BuildDetailViewModel
        {
            Build                = build,
            ConvertedPrice       = convertedPrice,
            CurrencyCode         = currencyCode.ToUpper(),
            EnrichedComponents   = enrichedComponents,
            ComponentPricesUsd   = componentPricesUsd,
            ComponentPricesEur   = componentPricesEur,
        };
    }

    // ── Personalização ────────────────────────────────────────────────────────

    public Task<CustomizeViewModel> GetCustomizeViewModelAsync(int? sourceBuildId, string? sessionId) =>
        _buildService.BuildCustomizeViewModelAsync(sourceBuildId, sessionId);

    public Task<BuildViewModel?> GetCustomBuildBySessionAsync(string sessionId) =>
        _buildService.GetCustomBuildBySessionAsync(sessionId);

    public async Task<SaveCustomizationResult> SaveCustomizationAsync(
        string sessionId,
        int? cpuId, int? gpuId, int? ramId, int? storageId,
        int? motherboardId, int? psuId, int? coolerId)
    {
        async Task<Product?> Fetch(int? id) => id.HasValue ? await _productRepo.GetByIdAsync(id.Value) : null;

        var cpu         = await Fetch(cpuId);
        var gpu         = await Fetch(gpuId);
        var ram         = await Fetch(ramId);
        var storage     = await Fetch(storageId);
        var motherboard = await Fetch(motherboardId);
        var psu         = await Fetch(psuId);
        var cooler      = await Fetch(coolerId);

        var errors = _compatibility.Validate(cpu, gpu, ram, storage, motherboard, psu, cooler);
        var price  = _pricing.Calculate(new[] { cpu, gpu, ram, storage, motherboard, psu, cooler });
        var power  = new[] { cpu, gpu, ram, storage, motherboard, psu, cooler }
                        .Where(p => p != null).Sum(p => p!.PowerConsumption);

        if (errors.Any(e => e.Severity == "error"))
            return new SaveCustomizationResult
            {
                IsValid = false, Errors = errors,
                SessionId = sessionId, EstimatedPrice = price, EstimatedPower = power
            };

        await _buildService.SaveCustomBuildAsync(
            sessionId, cpuId, gpuId, ramId, storageId, motherboardId, psuId, coolerId);

        return new SaveCustomizationResult
        {
            IsValid = true, Errors = errors,
            SessionId = sessionId, EstimatedPrice = price, EstimatedPower = power
        };
    }

    public Task<Build> CloneAsCustomAsync(int templateBuildId, string sessionId) =>
        _buildService.CloneAsCustomAsync(templateBuildId, sessionId);

    // ── Preço AJAX ────────────────────────────────────────────────────────────

    public async Task<LivePriceResult> GetLivePriceAsync(
        int? cpuId, int? gpuId, int? ramId, int? storageId,
        int? motherboardId, int? psuId, int? coolerId,
        string currencyCode = "BRL")
    {
        async Task<Product?> Fetch(int? id) => id.HasValue ? await _productRepo.GetByIdAsync(id.Value) : null;

        var products = new[]
        {
            await Fetch(cpuId), await Fetch(gpuId), await Fetch(ramId),
            await Fetch(storageId), await Fetch(motherboardId),
            await Fetch(psuId), await Fetch(coolerId)
        };

        var total  = _pricing.Calculate(products);
        var power  = products.Where(p => p != null).Sum(p => p!.PowerConsumption);
        var errors = _compatibility.Validate(
            products[0], products[1], products[2], products[3],
            products[4], products[5], products[6]);

        var componentCount = products.Count(p => p != null);

        // Decorator: aplica descontos
        var breakdown = GetPriceBreakdown(total, componentCount: componentCount);

        // Adapter: converte moeda
        string? converted = null;
        if (!string.Equals(currencyCode, "BRL", StringComparison.OrdinalIgnoreCase))
        {
            var adapter = _currencyFactory.GetAdapter(currencyCode);
            converted = adapter.Format(breakdown.FinalPrice);
        }

        // Preencher preços em USD e EUR no breakdown
        var usdAdapter = _currencyFactory.GetAdapter("USD");
        var eurAdapter = _currencyFactory.GetAdapter("EUR");
        breakdown.FinalPriceUsd = usdAdapter.Format(breakdown.FinalPrice);
        breakdown.FinalPriceEur = eurAdapter.Format(breakdown.FinalPrice);

        return new LivePriceResult
        {
            TotalFormatted  = total.ToString("C2", new System.Globalization.CultureInfo("pt-BR")),
            TotalRaw        = breakdown.FinalPrice,
            TotalConverted  = converted,
            CurrencyCode    = currencyCode.ToUpper(),
            Power           = power,
            Errors          = errors.Select(e => new CompatibilityErrorDto
            {
                Component = e.Component, Message = e.Message, Severity = e.Severity
            }).ToList(),
            PriceBreakdown  = breakdown,
        };
    }

    // ── Pedido ────────────────────────────────────────────────────────────────

    public async Task<OrderSummaryViewModel> GetOrderSummaryAsync(int? buildId, string? sessionId, string? couponCode = null)
    {
        BuildViewModel? buildVm = buildId.HasValue
            ? await _buildService.GetBuildViewModelAsync(buildId.Value)
            : (!string.IsNullOrEmpty(sessionId)
                ? await _buildService.GetCustomBuildBySessionAsync(sessionId)
                : null);

        if (buildVm == null) return new OrderSummaryViewModel();

        var build = buildId.HasValue
            ? await _buildRepo.GetByIdWithComponentsAsync(buildId.Value)
            : await _buildRepo.GetBySessionIdAsync(sessionId!);

        List<CompatibilityError> errors = [];
        if (build != null)
        {
            Product? Get(ComponentType t) =>
                build.Components.FirstOrDefault(c => c.Product.Type == t)?.Product;
            errors = _compatibility.Validate(
                Get(ComponentType.CPU), Get(ComponentType.GPU),
                Get(ComponentType.RAM), Get(ComponentType.Storage),
                Get(ComponentType.Motherboard), Get(ComponentType.PowerSupply),
                Get(ComponentType.Cooler));
        }

        var componentCount = new[]
        {
            buildVm.CPU, buildVm.GPU, buildVm.RAM, buildVm.Storage,
            buildVm.Motherboard, buildVm.PowerSupply, buildVm.Cooler
        }.Count(c => c != null);

        // Decorator: aplica descontos sobre o total da build
        var breakdown = GetPriceBreakdown(
            buildVm.TotalPrice,
            category: buildVm.Category == default ? null : buildVm.Category,
            couponCode: couponCode,
            componentCount: componentCount);

        // Adapter: preços em outras moedas
        var usdAdapter = _currencyFactory.GetAdapter("USD");
        var eurAdapter = _currencyFactory.GetAdapter("EUR");
        breakdown.FinalPriceUsd = usdAdapter.Format(breakdown.FinalPrice);
        breakdown.FinalPriceEur = eurAdapter.Format(breakdown.FinalPrice);

        return new OrderSummaryViewModel
        {
            BuildId            = buildId,
            SessionId          = sessionId,
            Build              = buildVm,
            CompatibilityErrors= errors,
            PriceBreakdown     = breakdown,
            CouponCode         = couponCode,
        };
    }

    public async Task<Order> PlaceOrderAsync(PlaceOrderRequest request)
    {
        Build? build = request.BuildId.HasValue
            ? await _buildRepo.GetByIdWithComponentsAsync(request.BuildId.Value)
            : await _buildRepo.GetBySessionIdAsync(request.SessionId);

        if (build == null)
            throw new InvalidOperationException("Build não encontrada para finalizar o pedido.");

        // Decorator: calcula preço com descontos
        var componentCount = build.Components.Count;
        var breakdown = _discountService.ApplyDiscounts(
            build.TotalPrice,
            category: build.IsTemplate ? build.Category : null,
            couponCode: request.CouponCode,
            componentCount: componentCount);

        var order = await _orderService.PlaceOrderAsync(
            build, request.CustomerName, request.CustomerEmail,
            request.CustomerPhone, request.ShippingAddress,
            finalPrice: breakdown.FinalPrice);

        await _notifier.NotifyOrderPlacedAsync(
            request.CustomerName, request.CustomerEmail,
            order.OrderNumber, order.TotalAmount);

        return order;
    }

    public Task<OrderConfirmationViewModel?> GetOrderConfirmationAsync(string orderNumber) =>
        _orderService.GetConfirmationAsync(orderNumber);

    // ── Componentes enriquecidos ───────────────────────────────────────────────

    public async Task<IEnumerable<EnrichedProductViewModel>> GetEnrichedProductsAsync(ComponentType type)
    {
        var products = await _productRepo.GetByTypeAsync(type);
        var usdAdapter = _currencyFactory.GetAdapter("USD");
        var eurAdapter = _currencyFactory.GetAdapter("EUR");

        return products.Select(p =>
        {
            var dto = _specAdapter.ToExternalDto(p);
            var vm  = _specAdapter.Enrich(Mapper.ToViewModel(p), dto);
            return vm;
        });
    }

    // ── Moedas ────────────────────────────────────────────────────────────────

    public IEnumerable<string> GetSupportedCurrencies() =>
        new[] { "BRL" }.Concat(_currencyFactory.SupportedCurrencies).Distinct();

    // ── Decorator ─────────────────────────────────────────────────────────────

    public PriceBreakdownViewModel GetPriceBreakdown(
        decimal basePrice,
        BuildCategory? category = null,
        string? couponCode = null,
        int componentCount = 0)
    {
        var result = _discountService.ApplyDiscounts(basePrice, category, couponCode, componentCount);

        return new PriceBreakdownViewModel
        {
            BasePrice       = result.BasePrice,
            FinalPrice      = result.FinalPrice,
            TotalDiscount   = result.TotalDiscount,
            DiscountPercent = result.DiscountPercent,
            AppliedDiscounts = result.AppliedDiscounts.Select(d => new AppliedDiscountViewModel
            {
                Label       = d.Label,
                Description = d.Description,
                Amount      = d.Amount,
                Percent     = d.Percent,
                Type        = d.Type,
            }).ToList(),
        };
    }

    public bool ValidateCoupon(string? code) => _discountService.ValidateCoupon(code);
}
