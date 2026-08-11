using PCBuilder.Models;

namespace PCBuilder.Patterns.Decorator;

// ═══════════════════════════════════════════════════════════════════════════════
// DECORATOR PATTERN
//
// Propósito: anexar responsabilidades adicionais a um objeto dinamicamente.
//
// O Decorator fornece uma alternativa flexível ao uso de herança para estender
// funcionalidades. Aqui, usamos o Decorator para aplicar vários descontos ao 
// cálculo de preço final sem modificar a lógica original de precificação.
// ═══════════════════════════════════════════════════════════════════════════════

public class AppliedDiscount
{
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percent { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class PriceBreakdown
{
    public decimal BasePrice { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal DiscountPercent => BasePrice > 0 ? (TotalDiscount / BasePrice) * 100 : 0;
    public List<AppliedDiscount> AppliedDiscounts { get; set; } = new();
    public string? FinalPriceUsd { get; set; }
    public string? FinalPriceEur { get; set; }
}

public interface IPriceCalculator
{
    PriceBreakdown Calculate();
}

public class BasePriceCalculator : IPriceCalculator
{
    private readonly decimal _basePrice;

    public BasePriceCalculator(decimal basePrice)
    {
        _basePrice = basePrice;
    }

    public PriceBreakdown Calculate()
    {
        return new PriceBreakdown
        {
            BasePrice = _basePrice,
            FinalPrice = _basePrice,
            TotalDiscount = 0,
            AppliedDiscounts = new List<AppliedDiscount>()
        };
    }
}

public abstract class PriceDecoratorBase : IPriceCalculator
{
    protected readonly IPriceCalculator _inner;

    protected PriceDecoratorBase(IPriceCalculator inner)
    {
        _inner = inner;
    }

    public virtual PriceBreakdown Calculate()
    {
        return _inner.Calculate();
    }
}

// ── Decorators Específicos ───────────────────────────────────────────────────

public class CategoryDiscountDecorator : PriceDecoratorBase
{
    private readonly BuildCategory _category;

    public CategoryDiscountDecorator(IPriceCalculator inner, BuildCategory category) 
        : base(inner)
    {
        _category = category;
    }

    public override PriceBreakdown Calculate()
    {
        var breakdown = base.Calculate();

        decimal discountPercent = _category switch
        {
            BuildCategory.GamerBase => 0.05m,
            BuildCategory.GamerPro => 0.10m,
            BuildCategory.Workstation => 0.15m,
            _ => 0m
        };

        if (discountPercent > 0)
        {
            decimal discountAmount = breakdown.FinalPrice * discountPercent;
            breakdown.FinalPrice -= discountAmount;
            breakdown.TotalDiscount += discountAmount;
            
            breakdown.AppliedDiscounts.Add(new AppliedDiscount
            {
                Label = $"Desconto {_category}",
                Description = $"Desconto aplicado pela categoria selecionada.",
                Amount = discountAmount,
                Percent = discountPercent * 100,
                Type = "Category"
            });
        }

        return breakdown;
    }
}

public class CouponDiscountDecorator : PriceDecoratorBase
{
    private readonly string? _couponCode;
    
    // Cupons válidos mockados para demonstração
    private readonly Dictionary<string, decimal> _validCoupons = new(StringComparer.OrdinalIgnoreCase)
    {
        { "DESCONTO10", 0.10m },
        { "GAMER20", 0.20m }
    };

    public CouponDiscountDecorator(IPriceCalculator inner, string? couponCode) 
        : base(inner)
    {
        _couponCode = couponCode;
    }

    public override PriceBreakdown Calculate()
    {
        var breakdown = base.Calculate();

        if (!string.IsNullOrEmpty(_couponCode) && _validCoupons.TryGetValue(_couponCode, out decimal discountPercent))
        {
            decimal discountAmount = breakdown.FinalPrice * discountPercent;
            breakdown.FinalPrice -= discountAmount;
            breakdown.TotalDiscount += discountAmount;
            
            breakdown.AppliedDiscounts.Add(new AppliedDiscount
            {
                Label = $"Cupom {_couponCode}",
                Description = $"Cupom promocional aplicado.",
                Amount = discountAmount,
                Percent = discountPercent * 100,
                Type = "Coupon"
            });
        }

        return breakdown;
    }
    
    public bool IsValid(string? code) => !string.IsNullOrEmpty(code) && _validCoupons.ContainsKey(code);
    public IEnumerable<string> GetValidCoupons() => _validCoupons.Keys;
}

public class BundleDiscountDecorator : PriceDecoratorBase
{
    private readonly int _componentCount;

    public BundleDiscountDecorator(IPriceCalculator inner, int componentCount) 
        : base(inner)
    {
        _componentCount = componentCount;
    }

    public override PriceBreakdown Calculate()
    {
        var breakdown = base.Calculate();

        // Se comprou todos os componentes principais (7 componentes), dá 5% extra
        if (_componentCount >= 7)
        {
            decimal discountPercent = 0.05m;
            decimal discountAmount = breakdown.FinalPrice * discountPercent;
            breakdown.FinalPrice -= discountAmount;
            breakdown.TotalDiscount += discountAmount;
            
            breakdown.AppliedDiscounts.Add(new AppliedDiscount
            {
                Label = "Desconto Bundle (PC Completo)",
                Description = "Desconto por adquirir todos os componentes.",
                Amount = discountAmount,
                Percent = discountPercent * 100,
                Type = "Bundle"
            });
        }

        return breakdown;
    }
}

public class LoyaltyDiscountDecorator : PriceDecoratorBase
{
    public LoyaltyDiscountDecorator(IPriceCalculator inner) : base(inner) { }

    public override PriceBreakdown Calculate()
    {
        var breakdown = base.Calculate();
        
        // Simulação de desconto fixo de fidelidade ou cash back (ex: R$50)
        decimal discountAmount = 50m;
        
        if (breakdown.FinalPrice >= discountAmount)
        {
            breakdown.FinalPrice -= discountAmount;
            breakdown.TotalDiscount += discountAmount;
            
            breakdown.AppliedDiscounts.Add(new AppliedDiscount
            {
                Label = "Desconto Fidelidade",
                Description = "Bônus especial de fidelidade.",
                Amount = discountAmount,
                Percent = 0,
                Type = "Loyalty"
            });
        }

        return breakdown;
    }
}

// ── Serviço Facade para os Descontos ──────────────────────────────────────────

public interface IDiscountService
{
    PriceBreakdown ApplyDiscounts(decimal basePrice, BuildCategory? category, string? couponCode, int componentCount);
    bool ValidateCoupon(string? code);
}

public class DiscountService : IDiscountService
{
    public PriceBreakdown ApplyDiscounts(decimal basePrice, BuildCategory? category, string? couponCode, int componentCount)
    {
        IPriceCalculator calculator = new BasePriceCalculator(basePrice);

        if (category.HasValue)
        {
            calculator = new CategoryDiscountDecorator(calculator, category.Value);
        }

        if (componentCount > 0)
        {
            calculator = new BundleDiscountDecorator(calculator, componentCount);
        }

        if (!string.IsNullOrEmpty(couponCode))
        {
            calculator = new CouponDiscountDecorator(calculator, couponCode);
        }

        // Exemplo: aplicar fidelidade sempre como último Decorator
        calculator = new LoyaltyDiscountDecorator(calculator);

        return calculator.Calculate();
    }

    public bool ValidateCoupon(string? code)
    {
        var fakeInner = new BasePriceCalculator(100);
        var couponDecorator = new CouponDiscountDecorator(fakeInner, code);
        return couponDecorator.IsValid(code);
    }
}
