using Microsoft.AspNetCore.Mvc;
using PCBuilder.Models;
using PCBuilder.Repositories;
using PCBuilder.Services;
using PCBuilder.ViewModels;

namespace PCBuilder.Controllers;

// ─── HomeController ──────────────────────────────────────────────────────────
public class HomeController : Controller
{
    private readonly IBuildService _buildService;
    public HomeController(IBuildService buildService) => _buildService = buildService;

    public async Task<IActionResult> Index()
    {
        var builds = await _buildService.GetAllTemplatesAsync();
        return View(builds);
    }
}

// ─── CatalogController ───────────────────────────────────────────────────────
public class CatalogController : Controller
{
    private readonly IBuildService _buildService;
    public CatalogController(IBuildService buildService) => _buildService = buildService;

    public async Task<IActionResult> Index()
    {
        var all = (await _buildService.GetAllTemplatesAsync()).ToList();
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
            "gamer-base"   => BuildCategory.GamerBase,
            "gamer-pro"    => BuildCategory.GamerPro,
            "workstation"  => BuildCategory.Workstation,
            _ => (BuildCategory?)null
        };
        if (category == null) return NotFound();
        var builds = await _buildService.GetTemplatesByCategoryAsync(category.Value);
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

    public async Task<IActionResult> BuildDetail(int id)
    {
        var build = await _buildService.GetBuildViewModelAsync(id);
        if (build == null) return NotFound();
        return View(build);
    }
}

// ─── BuildController ─────────────────────────────────────────────────────────
public class BuildController : Controller
{
    private readonly IBuildService _buildService;
    private readonly ICompatibilityService _compat;
    private readonly IPricingService _pricing;
    private readonly IProductRepository _productRepo;
    private readonly IBuildRepository _buildRepo;

    public BuildController(IBuildService buildService, ICompatibilityService compat,
        IPricingService pricing, IProductRepository productRepo, IBuildRepository buildRepo)
    {
        _buildService  = buildService;
        _compat        = compat;
        _pricing       = pricing;
        _productRepo   = productRepo;
        _buildRepo     = buildRepo;
    }

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
        var vm = await _buildService.BuildCustomizeViewModelAsync(sourceBuildId, session);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Customize(CustomizeViewModel form)
    {
        var session = GetOrCreateSession();

        // Load selected products for validation
        var cpu         = form.SelectedCpuId         != null ? await _productRepo.GetByIdAsync(form.SelectedCpuId.Value)         : null;
        var gpu         = form.SelectedGpuId         != null ? await _productRepo.GetByIdAsync(form.SelectedGpuId.Value)         : null;
        var ram         = form.SelectedRamId         != null ? await _productRepo.GetByIdAsync(form.SelectedRamId.Value)         : null;
        var storage     = form.SelectedStorageId     != null ? await _productRepo.GetByIdAsync(form.SelectedStorageId.Value)     : null;
        var motherboard = form.SelectedMotherboardId != null ? await _productRepo.GetByIdAsync(form.SelectedMotherboardId.Value) : null;
        var psu         = form.SelectedPsuId         != null ? await _productRepo.GetByIdAsync(form.SelectedPsuId.Value)         : null;
        var cooler      = form.SelectedCoolerId      != null ? await _productRepo.GetByIdAsync(form.SelectedCoolerId.Value)      : null;

        var errors = _compat.Validate(cpu, gpu, ram, storage, motherboard, psu, cooler);

        if (errors.Any(e => e.Severity == "error"))
        {
            var refreshedVm = await _buildService.BuildCustomizeViewModelAsync(form.SourceBuildId, session);
            refreshedVm.SelectedCpuId         = form.SelectedCpuId;
            refreshedVm.SelectedGpuId         = form.SelectedGpuId;
            refreshedVm.SelectedRamId         = form.SelectedRamId;
            refreshedVm.SelectedStorageId     = form.SelectedStorageId;
            refreshedVm.SelectedMotherboardId = form.SelectedMotherboardId;
            refreshedVm.SelectedPsuId         = form.SelectedPsuId;
            refreshedVm.SelectedCoolerId      = form.SelectedCoolerId;
            refreshedVm.CompatibilityErrors   = errors;
            refreshedVm.EstimatedPrice = _pricing.Calculate(new[] { cpu, gpu, ram, storage, motherboard, psu, cooler });
            return View(refreshedVm);
        }

        // Save build
        await _buildService.SaveCustomBuildAsync(session,
            form.SelectedCpuId, form.SelectedGpuId, form.SelectedRamId, form.SelectedStorageId,
            form.SelectedMotherboardId, form.SelectedPsuId, form.SelectedCoolerId);

        return RedirectToAction("Summary", "Order", new { sessionId = session });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloneAndReview(int buildId)
    {
        var session = GetOrCreateSession();
        await _buildService.CloneAsCustomAsync(buildId, session);
        return RedirectToAction("Summary", "Order", new { sessionId = session });
    }

    // AJAX endpoint for live price calculation
    [HttpGet]
    public async Task<IActionResult> GetPrice(int? cpuId, int? gpuId, int? ramId,
        int? storageId, int? motherboardId, int? psuId, int? coolerId)
    {
        var products = new List<Product?>();
        async Task<Product?> Get(int? id) => id.HasValue ? await _productRepo.GetByIdAsync(id.Value) : null;
        products.Add(await Get(cpuId));
        products.Add(await Get(gpuId));
        products.Add(await Get(ramId));
        products.Add(await Get(storageId));
        products.Add(await Get(motherboardId));
        products.Add(await Get(psuId));
        products.Add(await Get(coolerId));

        var cpu         = products[0];
        var gpu         = products[1];
        var ram         = products[2];
        var storage     = products[3];
        var motherboard = products[4];
        var psu         = products[5];
        var cooler      = products[6];

        var total  = _pricing.Calculate(products);
        var power  = products.Where(p => p != null).Sum(p => p!.PowerConsumption);
        var errors = _compat.Validate(cpu, gpu, ram, storage, motherboard, psu, cooler);

        return Json(new
        {
            total  = total.ToString("C2", new System.Globalization.CultureInfo("pt-BR")),
            totalRaw = total,
            power,
            errors = errors.Select(e => new { e.Component, e.Message, e.Severity })
        });
    }
}

// ─── OrderController ─────────────────────────────────────────────────────────
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IBuildService _buildService;
    private readonly IBuildRepository _buildRepo;
    private readonly ICompatibilityService _compat;

    public OrderController(IOrderService orderService, IBuildService buildService,
        IBuildRepository buildRepo, ICompatibilityService compat)
    {
        _orderService = orderService;
        _buildService = buildService;
        _buildRepo    = buildRepo;
        _compat       = compat;
    }

    public async Task<IActionResult> Summary(int? buildId, string? sessionId)
    {
        BuildViewModel? buildVm = null;

        if (buildId.HasValue)
            buildVm = await _buildService.GetBuildViewModelAsync(buildId.Value);
        else if (!string.IsNullOrEmpty(sessionId))
            buildVm = await _buildService.GetCustomBuildBySessionAsync(sessionId);

        if (buildVm == null) return RedirectToAction("Index", "Catalog");

        // Compute compatibility errors for summary
        var build = buildId.HasValue
            ? await _buildRepo.GetByIdWithComponentsAsync(buildId.Value)
            : await _buildRepo.GetBySessionIdAsync(sessionId!);

        List<CompatibilityError> errors = [];
        if (build != null)
        {
            Product? Get(ComponentType t) => build.Components.FirstOrDefault(c => c.Product.Type == t)?.Product;
            errors = _compat.Validate(Get(ComponentType.CPU), Get(ComponentType.GPU),
                Get(ComponentType.RAM), Get(ComponentType.Storage), Get(ComponentType.Motherboard),
                Get(ComponentType.PowerSupply), Get(ComponentType.Cooler));
        }

        var vm = new OrderSummaryViewModel
        {
            BuildId = buildId,
            SessionId = sessionId,
            Build = buildVm,
            CompatibilityErrors = errors
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(int? buildId, string? sessionId)
    {
        BuildViewModel? buildVm = null;
        if (buildId.HasValue)
            buildVm = await _buildService.GetBuildViewModelAsync(buildId.Value);
        else if (!string.IsNullOrEmpty(sessionId))
            buildVm = await _buildService.GetCustomBuildBySessionAsync(sessionId);

        if (buildVm == null) return RedirectToAction("Index", "Catalog");

        var vm = new CheckoutViewModel { BuildId = buildId, SessionId = sessionId, Build = buildVm };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel form)
    {
        BuildViewModel? buildVm = null;
        if (form.BuildId.HasValue)
            buildVm = await _buildService.GetBuildViewModelAsync(form.BuildId.Value);
        else if (!string.IsNullOrEmpty(form.SessionId))
            buildVm = await _buildService.GetCustomBuildBySessionAsync(form.SessionId);

        if (buildVm == null) return RedirectToAction("Index", "Catalog");
        form.Build = buildVm;

        if (!ModelState.IsValid) return View(form);

        Models.Build? build = null;
        if (form.BuildId.HasValue)
            build = await _buildRepo.GetByIdWithComponentsAsync(form.BuildId.Value);
        else if (!string.IsNullOrEmpty(form.SessionId))
            build = await _buildRepo.GetBySessionIdAsync(form.SessionId);

        if (build == null) return RedirectToAction("Index", "Catalog");

        var order = await _orderService.PlaceOrderAsync(build,
            form.CustomerName, form.CustomerEmail, form.CustomerPhone, form.ShippingAddress);

        HttpContext.Session.Remove("BuildSession");
        return RedirectToAction("Confirmation", new { orderNumber = order.OrderNumber });
    }

    public async Task<IActionResult> Confirmation(string orderNumber)
    {
        var vm = await _orderService.GetConfirmationAsync(orderNumber);
        if (vm == null) return NotFound();
        return View(vm);
    }
}
