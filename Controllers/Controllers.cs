using Microsoft.AspNetCore.Mvc;
using PCBuilder.Models;
using PCBuilder.Patterns.Facade;
using PCBuilder.ViewModels;

namespace PCBuilder.Controllers;

// ═══════════════════════════════════════════════════════════════════════════════
// CONTROLLERS — usam APENAS IPCBuilderFacade (padrão Facade aplicado)
//
// Antes: cada controller injetava 4-5 dependências (BuildService, CompatService,
//        PricingService, ProductRepo, BuildRepo...) e coordenava a lógica.
//
// Depois: injetam 1 dependência (IPCBuilderFacade) e chamam métodos de alto nível.
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

    // A rota convencional ({controller}/{action}/{id?}) nomeia o 3º segmento como
    // "id", então /Catalog/Category/gamer-base não preenchia "slug" e caía em 404.
    // Esta rota explícita aceita o slug no caminho (e ainda na query string).
    [HttpGet("Catalog/Category/{slug?}")]
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

    // Agora retorna BuildDetailViewModel com preço convertido + componentes enriquecidos
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

        // Facade orquestra: busca produtos + valida + salva — tudo em um método
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

    // AJAX — agora suporta conversão de moeda via CurrencyAdapter
    [HttpGet]
    public async Task<IActionResult> GetPrice(
        int? cpuId, int? gpuId, int? ramId,
        int? storageId, int? motherboardId, int? psuId, int? coolerId,
        string currency = "BRL")
    {
        var result = await _facade.GetLivePriceAsync(
            cpuId, gpuId, ramId, storageId, motherboardId, psuId, coolerId, currency);

        return Json(new
        {
            total          = result.TotalFormatted,
            totalRaw       = result.TotalRaw,
            totalConverted = result.TotalConverted,
            currencyCode   = result.CurrencyCode,
            power          = result.Power,
            errors         = result.Errors.Select(e => new
            {
                component = e.Component,
                message   = e.Message,
                severity  = e.Severity
            })
        });
    }

    // Nova rota: lista componentes enriquecidos com dados do fornecedor
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
    private const string MyOrdersKey = "MyOrders";

    private readonly IPCBuilderFacade _facade;
    public OrderController(IPCBuilderFacade facade) => _facade = facade;

    // ── Pedidos da sessão ─────────────────────────────────────────────────────
    // Sem login, a sessão é o que liga o visitante aos pedidos dele. Guardamos
    // apenas os números, para a página "Meus pedidos" conseguir listá-los depois.

    private List<string> GetSessionOrderNumbers()
    {
        var raw = HttpContext.Session.GetString(MyOrdersKey);
        return string.IsNullOrEmpty(raw)
            ? []
            : raw.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private void RememberOrder(string orderNumber)
    {
        var numbers = GetSessionOrderNumbers();
        if (numbers.Contains(orderNumber)) return;

        numbers.Add(orderNumber);
        HttpContext.Session.SetString(MyOrdersKey, string.Join('|', numbers));
    }

    public async Task<IActionResult> Summary(int? buildId, string? sessionId)
    {
        var vm = await _facade.GetOrderSummaryAsync(buildId, sessionId);
        if (vm.Build == null) return RedirectToAction("Index", "Catalog");
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(int? buildId, string? sessionId)
    {
        BuildViewModel? buildVm = buildId.HasValue
            ? await _facade.GetBuildViewModelAsync(buildId.Value)
            : (!string.IsNullOrEmpty(sessionId)
                ? await _facade.GetCustomBuildBySessionAsync(sessionId)
                : null);

        if (buildVm == null) return RedirectToAction("Index", "Catalog");

        return View(new CheckoutViewModel
        {
            BuildId   = buildId,
            SessionId = sessionId,
            Build     = buildVm
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

        // Facade encapsula: busca build + cria pedido + envia notificações
        var order = await _facade.PlaceOrderAsync(new PlaceOrderRequest
        {
            BuildId         = form.BuildId,
            SessionId       = form.SessionId ?? string.Empty,
            CustomerName    = form.CustomerName,
            CustomerEmail   = form.CustomerEmail,
            CustomerPhone   = form.CustomerPhone,
            ShippingAddress = form.ShippingAddress,
        });

        HttpContext.Session.Remove("BuildSession");
        RememberOrder(order.OrderNumber);   // para o cliente conseguir voltar ao pedido
        return RedirectToAction("Confirmation", new { orderNumber = order.OrderNumber });
    }

    // Lista os pedidos feitos nesta sessão + busca por número.
    public async Task<IActionResult> MyOrders(string? notFound)
    {
        var vm = await _facade.GetMyOrdersAsync(GetSessionOrderNumbers());
        vm.NotFoundNumber = notFound;
        return View(vm);
    }

    // Consulta por número: serve para voltar a um pedido de outra sessão
    // (sessão expirada, outro navegador) desde que se tenha o número.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Track(string orderNumber)
    {
        orderNumber = (orderNumber ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(orderNumber) || !await _facade.OrderExistsAsync(orderNumber))
            return RedirectToAction("MyOrders", new { notFound = orderNumber });

        RememberOrder(orderNumber);
        return RedirectToAction("Confirmation", new { orderNumber });
    }

    public async Task<IActionResult> Confirmation(string orderNumber)
    {
        var vm = await _facade.GetOrderConfirmationAsync(orderNumber);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // Avança o status do pedido para o próximo estado (State pattern).
    // Simula o painel operacional (confirmar pagamento → preparar → enviar → entregar).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdvanceStatus(string orderNumber)
    {
        await _facade.AdvanceOrderStatusAsync(orderNumber);
        return RedirectToAction("Confirmation", new { orderNumber });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(string orderNumber)
    {
        await _facade.CancelOrderAsync(orderNumber);
        return RedirectToAction("Confirmation", new { orderNumber });
    }
}
