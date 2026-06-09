using PCBuilder.Models;
using System.ComponentModel.DataAnnotations;

namespace PCBuilder.ViewModels;

public class CatalogViewModel
{
    public IEnumerable<BuildViewModel> GamerBaseBuilds { get; set; } = [];
    public IEnumerable<BuildViewModel> GamerProBuilds { get; set; } = [];
    public IEnumerable<BuildViewModel> WorkstationBuilds { get; set; } = [];
}

public class BuildViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BuildCategory Category { get; set; }
    public string CategoryLabel => Category switch
    {
        BuildCategory.GamerBase   => "PC Gamer Base",
        BuildCategory.GamerPro    => "PC Gamer Pro",
        BuildCategory.Workstation => "PC Workstation",
        _ => "Custom"
    };
    public decimal TotalPrice { get; set; }
    public int TotalPower { get; set; }
    public ProductViewModel? CPU { get; set; }
    public ProductViewModel? GPU { get; set; }
    public ProductViewModel? RAM { get; set; }
    public ProductViewModel? Storage { get; set; }
    public ProductViewModel? Motherboard { get; set; }
    public ProductViewModel? PowerSupply { get; set; }
    public ProductViewModel? Cooler { get; set; }
}

public class ProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ComponentType Type { get; set; }
    public int PowerConsumption { get; set; }
    public string? Socket { get; set; }
    public string? ChipsetCompatibility { get; set; }
    public int? TDP { get; set; }
    public int? WattageCapacity { get; set; }
}

public class CustomizeViewModel
{
    public int? SourceBuildId { get; set; }
    public string? SessionId { get; set; }

    public int? SelectedCpuId { get; set; }
    public int? SelectedGpuId { get; set; }
    public int? SelectedRamId { get; set; }
    public int? SelectedStorageId { get; set; }
    public int? SelectedMotherboardId { get; set; }
    public int? SelectedPsuId { get; set; }
    public int? SelectedCoolerId { get; set; }

    public IEnumerable<ProductViewModel> CPUs { get; set; } = [];
    public IEnumerable<ProductViewModel> GPUs { get; set; } = [];
    public IEnumerable<ProductViewModel> RAMs { get; set; } = [];
    public IEnumerable<ProductViewModel> Storages { get; set; } = [];
    public IEnumerable<ProductViewModel> Motherboards { get; set; } = [];
    public IEnumerable<ProductViewModel> PSUs { get; set; } = [];
    public IEnumerable<ProductViewModel> Coolers { get; set; } = [];

    public List<CompatibilityError> CompatibilityErrors { get; set; } = [];
    public decimal EstimatedPrice { get; set; }
    public int EstimatedPower { get; set; }
}

public class CompatibilityError
{
    public string Component { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "error"; // error | warning
}

public class OrderSummaryViewModel
{
    public int? BuildId { get; set; }
    public string? SessionId { get; set; }
    public BuildViewModel? Build { get; set; }
    public List<CompatibilityError> CompatibilityErrors { get; set; } = [];
    public bool IsValid => !CompatibilityErrors.Any(e => e.Severity == "error");

    // Decorator: desconto aplicado
    public PriceBreakdownViewModel? PriceBreakdown { get; set; }
    public string? CouponCode { get; set; }
}

public class CheckoutViewModel
{
    public int? BuildId { get; set; }
    public string? SessionId { get; set; }
    public BuildViewModel? Build { get; set; }

    [Required(ErrorMessage = "Nome é obrigatório")]
    [Display(Name = "Nome completo")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail é obrigatório")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [Display(Name = "E-mail")]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefone é obrigatório")]
    [Display(Name = "Telefone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Endereço é obrigatório")]
    [Display(Name = "Endereço de entrega")]
    public string ShippingAddress { get; set; } = string.Empty;

    // Decorator: cupom e breakdown de preço
    public string? CouponCode { get; set; }
    public PriceBreakdownViewModel? PriceBreakdown { get; set; }
}

public class OrderConfirmationViewModel
{
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = [];

    // Decorator: breakdown de descontos aplicados
    public List<AppliedDiscountViewModel> AppliedDiscounts { get; set; } = [];

    // Adapter: preço em outras moedas
    public string? TotalUsd { get; set; }
    public string? TotalEur { get; set; }
}

public class OrderItemViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public ComponentType Type { get; set; }
}

// ── ViewModels adicionados para suporte aos Padrões ──────────────────────────

/// <summary>ViewModel enriquecido com dados do fornecedor externo (via Adapter).</summary>
public class EnrichedProductViewModel : ProductViewModel
{
    public string SupplierSku        { get; set; } = string.Empty;
    public int    WarrantyMonths     { get; set; }
    public string AvailabilityStatus { get; set; } = string.Empty;
    public string AvailabilityCode   { get; set; } = string.Empty;
}

/// <summary>ViewModel de detalhe de build com preço convertido e componentes enriquecidos (via Facade + Adapters).</summary>
public class BuildDetailViewModel
{
    public BuildViewModel Build              { get; set; } = null!;
    public string?        ConvertedPrice     { get; set; }
    public string         CurrencyCode       { get; set; } = "BRL";
    public List<EnrichedProductViewModel> EnrichedComponents { get; set; } = [];

    // Preços convertidos por componente (via Adapter)
    public Dictionary<int, string> ComponentPricesUsd { get; set; } = [];
    public Dictionary<int, string> ComponentPricesEur { get; set; } = [];
}

// ── ViewModels para o Decorator de Descontos ──────────────────────────────────

/// <summary>Representa um desconto individual na tela.</summary>
public class AppliedDiscountViewModel
{
    public string Label       { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount     { get; set; }
    public decimal Percent    { get; set; }
    public string Type        { get; set; } = string.Empty;
}

/// <summary>Resumo completo de preço com descontos aplicados — usado em Summary, Checkout e Confirmação.</summary>
public class PriceBreakdownViewModel
{
    public decimal BasePrice       { get; set; }
    public decimal FinalPrice      { get; set; }
    public decimal TotalDiscount   { get; set; }
    public decimal DiscountPercent { get; set; }
    public List<AppliedDiscountViewModel> AppliedDiscounts { get; set; } = [];

    // Preço final em outras moedas (via Adapter)
    public string? FinalPriceUsd { get; set; }
    public string? FinalPriceEur { get; set; }

    public bool HasDiscount => TotalDiscount > 0;
}

/// <summary>Resultado da validação de cupom (retornado via AJAX).</summary>
public class CouponValidationViewModel
{
    public bool    IsValid    { get; set; }
    public string  Message    { get; set; } = string.Empty;
    public decimal Percent    { get; set; }
}
