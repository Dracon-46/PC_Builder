using PCBuilder.Models;
using PCBuilder.ViewModels;

namespace PCBuilder.Patterns.Decorators;


// ── Componente base — interface comum para preço calculado ────────────────────
public interface IPriceCalculator
{
    /// <summary>Calcula o preço total com todos os descontos aplicados.</summary>
    PriceBreakdown Calculate();
}

/// <summary>Resultado detalhado do cálculo de preço, com cada desconto discriminado.</summary>
public sealed class PriceBreakdown
{
    public decimal BasePrice        { get; init; }
    public decimal FinalPrice       { get; init; }
    public decimal TotalDiscount    { get; init; }
    public decimal DiscountPercent  => BasePrice > 0 ? Math.Round(TotalDiscount / BasePrice * 100, 1) : 0;
    public List<AppliedDiscount> AppliedDiscounts { get; init; } = [];

    // Preços convertidos (preenchidos pelo Facade quando há moeda selecionada)
    public string? FinalPriceUsd    { get; set; }
    public string? FinalPriceEur    { get; set; }
}

/// <summary>Representa um desconto individual aplicado no cálculo.</summary>
public sealed class AppliedDiscount
{
    public string Label         { get; init; } = string.Empty;
    public string Description   { get; init; } = string.Empty;
    public decimal Amount       { get; init; }
    public decimal Percent      { get; init; }
    public string Type          { get; init; } = string.Empty; // category | coupon | bundle | loyalty
}


// ── Implementação concreta base — preço puro sem descontos ───────────────────
public sealed class BasePriceCalculator : IPriceCalculator
{
    private readonly decimal _basePrice;

    public BasePriceCalculator(decimal basePrice) => _basePrice = basePrice;

    /// <summary>Constrói o calculador base somando os preços dos produtos fornecidos.</summary>
    public BasePriceCalculator(IEnumerable<Product?> products)
        => _basePrice = products.Where(p => p != null).Sum(p => p!.Price);

    public PriceBreakdown Calculate() => new()
    {
        BasePrice     = _basePrice,
        FinalPrice    = _basePrice,
        TotalDiscount = 0m,
        AppliedDiscounts = [],
    };
}


// ── Decorator abstrato — base para todos os decorators de desconto ────────────
public abstract class PriceDecoratorBase : IPriceCalculator
{
    protected readonly IPriceCalculator _inner;

    protected PriceDecoratorBase(IPriceCalculator inner) => _inner = inner;

    public abstract PriceBreakdown Calculate();

    /// <summary>
    /// Aplica um desconto percentual sobre o preço final atual e
    /// retorna um novo PriceBreakdown com o desconto adicionado à lista.
    /// </summary>
    protected static PriceBreakdown ApplyDiscount(
        PriceBreakdown current,
        decimal discountPercent,
        string label,
        string description,
        string type)
    {
        if (discountPercent <= 0) return current;

        var discountAmount = Math.Round(current.FinalPrice * discountPercent / 100m, 2);
        var newFinal       = current.FinalPrice - discountAmount;

        var newDiscounts = new List<AppliedDiscount>(current.AppliedDiscounts)
        {
            new()
            {
                Label       = label,
                Description = description,
                Amount      = discountAmount,
                Percent     = discountPercent,
                Type        = type,
            }
        };

        return new PriceBreakdown
        {
            BasePrice        = current.BasePrice,
            FinalPrice       = Math.Max(0, newFinal),
            TotalDiscount    = current.TotalDiscount + discountAmount,
            AppliedDiscounts = newDiscounts,
        };
    }
}


// ── Decorator 1: Desconto por Categoria ──────────────────────────────────────
/// <summary>
/// Aplica desconto fixo conforme a categoria da build.
/// GamerBase   → 5%  (estimula iniciantes)
/// GamerPro    → 3%  (desconto menor, margem menor)
/// Workstation → 7%  (volume maior, desconto maior)
/// </summary>
public sealed class CategoryDiscountDecorator : PriceDecoratorBase
{
    private readonly BuildCategory _category;

    // Tabela de descontos por categoria
    private static readonly Dictionary<BuildCategory, (decimal Percent, string Label)> _discounts = new()
    {
        [BuildCategory.GamerBase]   = (5m,  "Desconto Gamer Base"),
        [BuildCategory.GamerPro]    = (3m,  "Desconto Gamer Pro"),
        [BuildCategory.Workstation] = (7m,  "Desconto Workstation"),
    };

    public CategoryDiscountDecorator(IPriceCalculator inner, BuildCategory category)
        : base(inner) => _category = category;

    public override PriceBreakdown Calculate()
    {
        var current = _inner.Calculate();

        if (!_discounts.TryGetValue(_category, out var disc)) return current;

        return ApplyDiscount(
            current,
            discountPercent: disc.Percent,
            label:           disc.Label,
            description:     $"{disc.Percent}% de desconto para builds da categoria {_category}",
            type:            "category");
    }
}


// ── Decorator 2: Desconto por Cupom ──────────────────────────────────────────
/// <summary>
/// Aplica desconto mediante código de cupom válido.
/// Cupons disponíveis (para fins de portfólio):
///   PCBUILDER10 → 10%
///   PROMO15     → 15%
///   BEMVINDO5   → 5%
/// </summary>
public sealed class CouponDiscountDecorator : PriceDecoratorBase
{
    private readonly string? _couponCode;

    // Tabela de cupons: código → (percentual, descrição)
    private static readonly Dictionary<string, (decimal Percent, string Description)> _coupons =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["PCBUILDER10"] = (10m, "Cupom de boas-vindas PCBuilder"),
            ["PROMO15"]     = (15m, "Promoção especial de lançamento"),
            ["BEMVINDO5"]   = (5m,  "Desconto de primeiro pedido"),
        };

    public CouponDiscountDecorator(IPriceCalculator inner, string? couponCode)
        : base(inner) => _couponCode = couponCode?.Trim();

    public override PriceBreakdown Calculate()
    {
        var current = _inner.Calculate();

        if (string.IsNullOrWhiteSpace(_couponCode)) return current;
        if (!_coupons.TryGetValue(_couponCode, out var coupon)) return current;

        return ApplyDiscount(
            current,
            discountPercent: coupon.Percent,
            label:           $"Cupom: {_couponCode.ToUpper()}",
            description:     $"{coupon.Description} ({coupon.Percent}%)",
            type:            "coupon");
    }

    /// <summary>Valida se um cupom existe sem precisar instanciar o decorator.</summary>
    public static bool IsValid(string? code) =>
        !string.IsNullOrWhiteSpace(code) && _coupons.ContainsKey(code.Trim());

    /// <summary>Lista todos os cupons válidos (útil para telas de ajuda/documentação).</summary>
    public static IEnumerable<string> GetValidCoupons() => _coupons.Keys;
}


// ── Decorator 3: Desconto por Bundle (número de componentes) ─────────────────
/// <summary>
/// Desconto progressivo conforme o número de componentes na build.
/// 5 componentes → 2%
/// 6 componentes → 3%
/// 7 componentes → 5%  (build completa)
/// Incentiva o cliente a montar a build completa.
/// </summary>
public sealed class BundleDiscountDecorator : PriceDecoratorBase
{
    private readonly int _componentCount;

    // Tabela de descontos por faixa de componentes
    private static readonly (int MinComponents, decimal Percent, string Label)[] _tiers =
    [
        (7, 5m, "Build Completa (7 componentes)"),
        (6, 3m, "Quase completa (6 componentes)"),
        (5, 2m, "Build parcial (5 componentes)"),
    ];

    public BundleDiscountDecorator(IPriceCalculator inner, int componentCount)
        : base(inner) => _componentCount = componentCount;

    public override PriceBreakdown Calculate()
    {
        var current = _inner.Calculate();

        // Encontra a maior faixa aplicável
        var tier = _tiers.FirstOrDefault(t => _componentCount >= t.MinComponents);
        if (tier.MinComponents == 0) return current;

        return ApplyDiscount(
            current,
            discountPercent: tier.Percent,
            label:           $"Desconto Bundle — {tier.Label}",
            description:     $"{tier.Percent}% de desconto por selecionar {_componentCount} componentes",
            type:            "bundle");
    }
}


// ── Decorator 4: Desconto Fidelidade (carrinho acima de limiar) ───────────────
/// <summary>
/// Bônus de fidelidade: builds acima de R$ 8.000 ganham 2% adicional.
/// Recompensa compras de maior valor agregado.
/// </summary>
public sealed class LoyaltyDiscountDecorator : PriceDecoratorBase
{
    private const decimal Threshold = 8_000m;
    private const decimal DiscountPercent = 2m;

    public LoyaltyDiscountDecorator(IPriceCalculator inner) : base(inner) { }

    public override PriceBreakdown Calculate()
    {
        var current = _inner.Calculate();

        if (current.FinalPrice < Threshold) return current;

        return ApplyDiscount(
            current,
            discountPercent: DiscountPercent,
            label:           "Bônus Fidelidade",
            description:     $"{DiscountPercent}% de bônus para builds acima de R$ {Threshold:N0}",
            type:            "loyalty");
    }
}


// ── Serviço orquestrador de descontos ─────────────────────────────────────────
/// <summary>
/// Monta a cadeia de decorators na ordem correta e executa o cálculo.
/// Injetado via DI — os Controllers e a Facade usam este serviço.
/// </summary>
public interface IDiscountService
{
    PriceBreakdown ApplyDiscounts(
        decimal basePrice,
        BuildCategory? category = null,
        string? couponCode = null,
        int componentCount = 0);

    bool ValidateCoupon(string? code);
}

public sealed class DiscountService : IDiscountService
{
    public PriceBreakdown ApplyDiscounts(
        decimal basePrice,
        BuildCategory? category = null,
        string? couponCode = null,
        int componentCount = 0)
    {
        // Monta a cadeia de decorators
        IPriceCalculator calc = new BasePriceCalculator(basePrice);

        // 1. Desconto de categoria (se build template)
        if (category.HasValue)
            calc = new CategoryDiscountDecorator(calc, category.Value);

        // 2. Desconto de bundle (por número de peças selecionadas)
        if (componentCount > 0)
            calc = new BundleDiscountDecorator(calc, componentCount);

        // 3. Desconto de cupom
        if (!string.IsNullOrWhiteSpace(couponCode))
            calc = new CouponDiscountDecorator(calc, couponCode);

        // 4. Bônus de fidelidade (último, sobre preço já descontado)
        calc = new LoyaltyDiscountDecorator(calc);

        return calc.Calculate();
    }

    public bool ValidateCoupon(string? code) => CouponDiscountDecorator.IsValid(code);
}
