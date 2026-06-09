using Microsoft.AspNetCore.Mvc;
using PCBuilder.Models;
using PCBuilder.Patterns.Facade;
using PCBuilder.ViewModels;

namespace PCBuilder.Controllers;

// ═══════════════════════════════════════════════════════════════════════════════
// CONTROLLERS — usam APENAS IPCBuilderFacade (padrão Facade aplicado)
// ═══════════════════════════════════════════════════════════════════════════════

// ─── HomeController ──────────────────────────────────────────────────────────
public class HomeController : Controller
{
    private readonly IPCBuilderFacade _facade;
    public HomeController(IPCBuilderFacade facade) => _facade = facade;

    public async Task<IActionResult> Index()
    {
        var builds = await _facade.GetAllTemplatesAsync();
        return View(builds);
    }
}

// ─── CatalogController ───────────────────────────────────────────────────────
public class CatalogController : Controller
{
    private readonly IPCBuilderFacade _facade;
    public CatalogController(IPCBuilderFacade facade) => _facade = facade;

    public async Task<IActionResult> Index()
    {
        var all = (await _facade.GetAllTemplatesAsync()).ToList();
        var vm = new CatalogViewModel
        {
            GamerBaseBuilds   = all.Where(b => b.Category == BuildCategory.GamerBase),
            GamerProBuilds    = all.Where(b => b.Category == BuildCategory.GamerPro),
            WorkstationBuilds = all.Where(b => b.Category == BuildCategory.Workstation)
        };
        return View(vm);
    }

    public async Task<IActionResult> Category(string slug)
    {
        var category = slug switch
        {
            "gamer-base"  => BuildCategory.GamerBase,
            "gamer-pro"   => BuildCategory.GamerPro,
            "workstation" => BuildCategory.Workstation,
            _ => (BuildCategory?)null
        };
        if (category == null) return NotFound();

        var builds = await _facade.GetTemplatesByCategoryAsync(category.Value);
        ViewBag.CategorySlug = slug;
        ViewBag.CategoryName = slug switch
        {
            "gamer-base"  => "PC Gamer Base",
            "gamer-pro"   => "PC Gamer Pro",
            "workstation" => "PC Workstation",
            _ => slug
        };
        return View(builds);
    }

    public async Task<IActionResult> BuildDetail(int id, string currency = "BRL")
    {
        var vm = await _facade.GetBuildDetailAsync(id, currency);
        if (vm == null) return NotFound();
        ViewBag.SupportedCurrencies = _facade.GetSupportedCurrencies();
        return View(vm);
    }
}

// ─── BuildController ─────────────────────────────────────────────────────────
public class BuildController : Controller
{
    private readonly IPCBuilderFacade _facade;
    public BuildController(IPCBuilderFacade facade) => _facade = facade;

    private string GetOrCreateSession()
    {
        var session = HttpContext.Session.GetString("BuildSession");
        if (string.IsNullOrEmpty(session))
        {
            session = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString("BuildSession", session);
        }
        return session;
    }

    [HttpGet]
    public async Task<IActionResult> Customize(int? sourceBuildId)
    {
        var session = GetOrCreateSession();
        var vm = await _facade.GetCustomizeViewModelAsync(sourceBuildId, session);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Customize(CustomizeViewModel form)
    {
        var session = GetOrCreateSession();

        var result = await _facade.SaveCustomizationAsync(
            session,
            form.SelectedCpuId, form.SelectedGpuId, form.SelectedRamId,
            form.SelectedStorageId, form.SelectedMotherboardId,
            form.SelectedPsuId, form.SelectedCoolerId);

        if (!result.IsValid)
        {
            var refreshedVm = await _facade.GetCustomizeViewModelAsync(form.SourceBuildId, session);
            refreshedVm.SelectedCpuId         = form.SelectedCpuId;
            refreshedVm.SelectedGpuId         = form.SelectedGpuId;
            refreshedVm.SelectedRamId         = form.SelectedRamId;
            refreshedVm.SelectedStorageId     = form.SelectedStorageId;
            refreshedVm.SelectedMotherboardId = form.SelectedMotherboardId;
            refreshedVm.SelectedPsuId         = form.SelectedPsuId;
            refreshedVm.SelectedCoolerId      = form.SelectedCoolerId;
            refreshedVm.CompatibilityErrors   = result.Errors;
            refreshedVm.EstimatedPrice        = result.EstimatedPrice;
            refreshedVm.EstimatedPower        = result.EstimatedPower;
            return View(refreshedVm);
        }

        return RedirectToAction("Summary", "Order", new { sessionId = session });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloneAndReview(int buildId)
    {
        var session = GetOrCreateSession();
        await _facade.CloneAsCustomAsync(buildId, session);
        return RedirectToAction("Summary", "Order", new { sessionId = session });
    }

    // AJAX — retorna preço com descontos (Decorator) e conversão de moeda (Adapter)
    [HttpGet]
    public async Task<IActionResult> GetPrice(
        int? cpuId, int? gpuId, int? ramId,
        int? storageId, int? motherboardId, int? psuId, int? coolerId,
        string currency = "BRL",
        string? coupon = null)
    {
        var result = await _facade.GetLivePriceAsync(
            cpuId, gpuId, ramId, storageId, motherboardId, psuId, coolerId, currency);

        // Aplicar cupom no resultado AJAX, se informado
        var bd = result.PriceBreakdown;
        if (!string.IsNullOrWhiteSpace(coupon) && bd != null)
        {
            var componentCount = new[] { cpuId, gpuId, ramId, storageId, motherboardId, psuId, coolerId }
                .Count(id => id.HasValue);
            bd = _facade.GetPriceBreakdown(result.PriceBreakdown!.BasePrice, couponCode: coupon, componentCount: componentCount);
        }

        return Json(new
        {
            total           = result.TotalFormatted,
            totalRaw        = result.TotalRaw,
            totalConverted  = result.TotalConverted,
            currencyCode    = result.CurrencyCode,
            power           = result.Power,
            errors          = result.Errors.Select(e => new
            {
                component = e.Component,
                message   = e.Message,
                severity  = e.Severity
            }),
            // Decorator: breakdown de descontos
            priceBreakdown  = bd == null ? null : new
            {
                basePrice       = bd.BasePrice,
                finalPrice      = bd.FinalPrice,
                totalDiscount   = bd.TotalDiscount,
                discountPercent = bd.DiscountPercent,
                finalPriceUsd   = bd.FinalPriceUsd,
                finalPriceEur   = bd.FinalPriceEur,
                hasDiscount     = bd.HasDiscount,
                discounts       = bd.AppliedDiscounts.Select(d => new
                {
                    label       = d.Label,
                    description = d.Description,
                    amount      = d.Amount,
                    percent     = d.Percent,
                    type        = d.Type,
                })
            }
        });
    }

    // AJAX — valida cupom
    [HttpGet]
    public IActionResult ValidateCoupon(string? code)
    {
        var isValid = _facade.ValidateCoupon(code);
        return Json(new
        {
            isValid,
            message = isValid ? "Cupom válido! Desconto aplicado." : "Cupom inválido ou expirado.",
        });
    }

    [HttpGet]
    public async Task<IActionResult> Components(string type)
    {
        if (!Enum.TryParse<ComponentType>(type, true, out var componentType))
            return BadRequest("Tipo inválido.");

        var enriched = await _facade.GetEnrichedProductsAsync(componentType);
        return Json(enriched.Select(e => new
        {
            e.Id, e.Name, e.Brand, e.Description, e.Price,
            e.PowerConsumption, e.Socket, e.TDP, e.WattageCapacity,
            e.SupplierSku, e.WarrantyMonths,
            e.AvailabilityStatus, e.AvailabilityCode
        }));
    }
}

// ─── OrderController ─────────────────────────────────────────────────────────
public class OrderController : Controller
{
    private readonly IPCBuilderFacade _facade;
    public OrderController(IPCBuilderFacade facade) => _facade = facade;

    public async Task<IActionResult> Summary(int? buildId, string? sessionId, string? coupon = null)
    {
        var vm = await _facade.GetOrderSummaryAsync(buildId, sessionId, coupon);
        if (vm.Build == null) return RedirectToAction("Index", "Catalog");
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(int? buildId, string? sessionId, string? coupon = null)
    {
        BuildViewModel? buildVm = buildId.HasValue
            ? await _facade.GetBuildViewModelAsync(buildId.Value)
            : (!string.IsNullOrEmpty(sessionId)
                ? await _facade.GetCustomBuildBySessionAsync(sessionId)
                : null);

        if (buildVm == null) return RedirectToAction("Index", "Catalog");

        // Decorator: calcula desconto para a tela de checkout
        var componentCount = new[]
        {
            buildVm.CPU, buildVm.GPU, buildVm.RAM, buildVm.Storage,
            buildVm.Motherboard, buildVm.PowerSupply, buildVm.Cooler
        }.Count(c => c != null);

        var breakdown = _facade.GetPriceBreakdown(
            buildVm.TotalPrice,
            category: buildVm.Category == default ? null : buildVm.Category,
            couponCode: coupon,
            componentCount: componentCount);

        return View(new CheckoutViewModel
        {
            BuildId        = buildId,
            SessionId      = sessionId,
            Build          = buildVm,
            CouponCode     = coupon,
            PriceBreakdown = breakdown,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel form)
    {
        BuildViewModel? buildVm = form.BuildId.HasValue
            ? await _facade.GetBuildViewModelAsync(form.BuildId.Value)
            : (!string.IsNullOrEmpty(form.SessionId)
                ? await _facade.GetCustomBuildBySessionAsync(form.SessionId)
                : null);

        if (buildVm == null) return RedirectToAction("Index", "Catalog");
        form.Build = buildVm;

        if (!ModelState.IsValid) return View(form);

        var order = await _facade.PlaceOrderAsync(new PlaceOrderRequest
        {
            BuildId         = form.BuildId,
            SessionId       = form.SessionId ?? string.Empty,
            CustomerName    = form.CustomerName,
            CustomerEmail   = form.CustomerEmail,
            CustomerPhone   = form.CustomerPhone,
            ShippingAddress = form.ShippingAddress,
            CouponCode      = form.CouponCode,
            Category        = buildVm.Category,
        });

        HttpContext.Session.Remove("BuildSession");
        return RedirectToAction("Confirmation", new { orderNumber = order.OrderNumber });
    }

    public async Task<IActionResult> Confirmation(string orderNumber)
    {
        var vm = await _facade.GetOrderConfirmationAsync(orderNumber);
        if (vm == null) return NotFound();
        return View(vm);
    }
}
