using PCBuilder.Models;
using PCBuilder.Repositories;
using PCBuilder.ViewModels;

namespace PCBuilder.Services;

// ─── Mapping helpers ────────────────────────────────────────────────────────
public static class Mapper
{
    public static ProductViewModel ToViewModel(Product p) => new()
    {
        Id = p.Id, Name = p.Name, Brand = p.Brand, Description = p.Description,
        Price = p.Price, Type = p.Type, PowerConsumption = p.PowerConsumption,
        Socket = p.Socket, ChipsetCompatibility = p.ChipsetCompatibility,
        TDP = p.TDP, WattageCapacity = p.WattageCapacity
    };

    public static BuildViewModel ToViewModel(Build b)
    {
        var vm = new BuildViewModel
        {
            Id = b.Id, Name = b.Name, Description = b.Description, Category = b.Category,
            TotalPrice = b.TotalPrice, TotalPower = b.TotalPower
        };
        foreach (var bc in b.Components)
        {
            var pvm = ToViewModel(bc.Product);
            switch (bc.Product.Type)
            {
                case ComponentType.CPU:          vm.CPU          = pvm; break;
                case ComponentType.GPU:          vm.GPU          = pvm; break;
                case ComponentType.RAM:          vm.RAM          = pvm; break;
                case ComponentType.Storage:      vm.Storage      = pvm; break;
                case ComponentType.Motherboard:  vm.Motherboard  = pvm; break;
                case ComponentType.PowerSupply:  vm.PowerSupply  = pvm; break;
                case ComponentType.Cooler:       vm.Cooler       = pvm; break;
            }
        }
        return vm;
    }
}

// ─── CompatibilityService ────────────────────────────────────────────────────
public interface ICompatibilityService
{
    List<CompatibilityError> Validate(
        Product? cpu, Product? gpu, Product? ram,
        Product? storage, Product? motherboard,
        Product? psu, Product? cooler);
}

public class CompatibilityService : ICompatibilityService
{
    public List<CompatibilityError> Validate(
        Product? cpu, Product? gpu, Product? ram,
        Product? storage, Product? motherboard,
        Product? psu, Product? cooler)
    {
        var errors = new List<CompatibilityError>();

        // CPU ↔ Motherboard socket
        if (cpu != null && motherboard != null)
        {
            if (!string.IsNullOrEmpty(cpu.Socket) &&
                !string.IsNullOrEmpty(motherboard.Socket) &&
                !cpu.Socket.Equals(motherboard.Socket, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new CompatibilityError
                {
                    Component = "CPU / Placa-mãe",
                    Message = $"Socket incompatível: CPU usa {cpu.Socket}, placa-mãe suporta {motherboard.Socket}.",
                    Severity = "error"
                });
            }
        }

        // PSU wattage check
        if (psu != null)
        {
            int totalPower = 0;
            if (cpu != null) totalPower += cpu.PowerConsumption;
            if (gpu != null) totalPower += gpu.PowerConsumption;
            if (ram != null) totalPower += ram.PowerConsumption;
            if (storage != null) totalPower += storage.PowerConsumption;
            if (motherboard != null) totalPower += motherboard.PowerConsumption;
            if (cooler != null) totalPower += cooler.PowerConsumption;

            int recommended = (int)(totalPower * 1.20); // 20% headroom

            if (psu.WattageCapacity.HasValue && psu.WattageCapacity.Value < recommended)
            {
                errors.Add(new CompatibilityError
                {
                    Component = "Fonte",
                    Message = $"Fonte de {psu.WattageCapacity}W pode ser insuficiente. Consumo estimado: {totalPower}W (recomendado {recommended}W com margem de segurança).",
                    Severity = psu.WattageCapacity.Value < totalPower ? "error" : "warning"
                });
            }
        }

        // Cooler TDP check
        if (cooler != null && cpu != null)
        {
            if (cooler.TDP.HasValue && cpu.TDP.HasValue && cooler.TDP.Value < cpu.TDP.Value)
            {
                errors.Add(new CompatibilityError
                {
                    Component = "Cooler",
                    Message = $"Cooler suporta até {cooler.TDP}W TDP, mas o CPU tem {cpu.TDP}W TDP. Risco de throttling térmico.",
                    Severity = "warning"
                });
            }
        }

        return errors;
    }
}

// ─── PricingService ──────────────────────────────────────────────────────────
public interface IPricingService
{
    decimal Calculate(IEnumerable<Product?> products);
}

public class PricingService : IPricingService
{
    public decimal Calculate(IEnumerable<Product?> products) =>
        products.Where(p => p != null).Sum(p => p!.Price);
}

// ─── BuildService ────────────────────────────────────────────────────────────
public interface IBuildService
{
    Task<IEnumerable<BuildViewModel>> GetAllTemplatesAsync();
    Task<IEnumerable<BuildViewModel>> GetTemplatesByCategoryAsync(BuildCategory category);
    Task<BuildViewModel?> GetBuildViewModelAsync(int buildId);
    Task<BuildViewModel?> GetCustomBuildBySessionAsync(string sessionId);
    Task<Build> CloneAsCustomAsync(int templateBuildId, string sessionId);
    Task<Build> SaveCustomBuildAsync(string sessionId,
        int? cpuId, int? gpuId, int? ramId, int? storageId,
        int? motherboardId, int? psuId, int? coolerId);
    Task<CustomizeViewModel> BuildCustomizeViewModelAsync(int? sourceBuildId, string? sessionId);
}

public class BuildService : IBuildService
{
    private readonly IBuildRepository _buildRepo;
    private readonly IProductRepository _productRepo;
    private readonly ICompatibilityService _compat;
    private readonly IPricingService _pricing;

    public BuildService(IBuildRepository buildRepo, IProductRepository productRepo,
        ICompatibilityService compat, IPricingService pricing)
    {
        _buildRepo  = buildRepo;
        _productRepo = productRepo;
        _compat     = compat;
        _pricing    = pricing;
    }

    public async Task<IEnumerable<BuildViewModel>> GetAllTemplatesAsync()
    {
        var builds = await _buildRepo.GetTemplatesAsync();
        return builds.Select(Mapper.ToViewModel);
    }

    public async Task<IEnumerable<BuildViewModel>> GetTemplatesByCategoryAsync(BuildCategory category)
    {
        var builds = await _buildRepo.GetTemplatesByCategoryAsync(category);
        return builds.Select(Mapper.ToViewModel);
    }

    public async Task<BuildViewModel?> GetBuildViewModelAsync(int buildId)
    {
        var build = await _buildRepo.GetByIdWithComponentsAsync(buildId);
        return build == null ? null : Mapper.ToViewModel(build);
    }

    public async Task<BuildViewModel?> GetCustomBuildBySessionAsync(string sessionId)
    {
        var build = await _buildRepo.GetBySessionIdAsync(sessionId);
        return build == null ? null : Mapper.ToViewModel(build);
    }

    public async Task<Build> CloneAsCustomAsync(int templateBuildId, string sessionId)
    {
        var template = await _buildRepo.GetByIdWithComponentsAsync(templateBuildId)
            ?? throw new InvalidOperationException("Template build not found");

        // Remove old custom build for this session if any
        var existing = await _buildRepo.GetBySessionIdAsync(sessionId);
        if (existing != null) await _buildRepo.DeleteAsync(existing.Id);

        var clone = new Build
        {
            Name = $"{template.Name} (Personalizada)",
            Description = template.Description,
            Category = template.Category,
            IsTemplate = false,
            IsCustom = true,
            SessionId = sessionId,
            Components = template.Components.Select(c => new BuildComponent
            {
                ProductId = c.ProductId,
                Quantity = c.Quantity
            }).ToList()
        };
        return await _buildRepo.CreateAsync(clone);
    }

    public async Task<Build> SaveCustomBuildAsync(string sessionId,
        int? cpuId, int? gpuId, int? ramId, int? storageId,
        int? motherboardId, int? psuId, int? coolerId)
    {
        var existing = await _buildRepo.GetBySessionIdAsync(sessionId);
        if (existing != null) await _buildRepo.DeleteAsync(existing.Id);

        var components = new List<BuildComponent>();
        void AddIf(int? id) { if (id.HasValue) components.Add(new BuildComponent { ProductId = id.Value, Quantity = 1 }); }
        AddIf(cpuId); AddIf(gpuId); AddIf(ramId); AddIf(storageId);
        AddIf(motherboardId); AddIf(psuId); AddIf(coolerId);

        var build = new Build
        {
            Name = "PC Personalizado",
            Description = "Build criada pelo usuário",
            Category = BuildCategory.GamerBase,
            IsTemplate = false,
            IsCustom = true,
            SessionId = sessionId,
            Components = components
        };
        return await _buildRepo.CreateAsync(build);
    }

    public async Task<CustomizeViewModel> BuildCustomizeViewModelAsync(int? sourceBuildId, string? sessionId)
    {
        var cpus         = (await _productRepo.GetByTypeAsync(ComponentType.CPU)).Select(Mapper.ToViewModel).ToList();
        var gpus         = (await _productRepo.GetByTypeAsync(ComponentType.GPU)).Select(Mapper.ToViewModel).ToList();
        var rams         = (await _productRepo.GetByTypeAsync(ComponentType.RAM)).Select(Mapper.ToViewModel).ToList();
        var storages     = (await _productRepo.GetByTypeAsync(ComponentType.Storage)).Select(Mapper.ToViewModel).ToList();
        var motherboards = (await _productRepo.GetByTypeAsync(ComponentType.Motherboard)).Select(Mapper.ToViewModel).ToList();
        var psus         = (await _productRepo.GetByTypeAsync(ComponentType.PowerSupply)).Select(Mapper.ToViewModel).ToList();
        var coolers      = (await _productRepo.GetByTypeAsync(ComponentType.Cooler)).Select(Mapper.ToViewModel).ToList();

        var vm = new CustomizeViewModel
        {
            SourceBuildId = sourceBuildId,
            SessionId = sessionId,
            CPUs = cpus, GPUs = gpus, RAMs = rams, Storages = storages,
            Motherboards = motherboards, PSUs = psus, Coolers = coolers
        };

        // Pre-select from source build or existing session build
        Build? sourceBuild = null;
        if (sourceBuildId.HasValue)
            sourceBuild = await _buildRepo.GetByIdWithComponentsAsync(sourceBuildId.Value);
        else if (!string.IsNullOrEmpty(sessionId))
            sourceBuild = await _buildRepo.GetBySessionIdAsync(sessionId);

        if (sourceBuild != null)
        {
            foreach (var bc in sourceBuild.Components)
            {
                switch (bc.Product.Type)
                {
                    case ComponentType.CPU:         vm.SelectedCpuId         = bc.ProductId; break;
                    case ComponentType.GPU:         vm.SelectedGpuId         = bc.ProductId; break;
                    case ComponentType.RAM:         vm.SelectedRamId         = bc.ProductId; break;
                    case ComponentType.Storage:     vm.SelectedStorageId     = bc.ProductId; break;
                    case ComponentType.Motherboard: vm.SelectedMotherboardId = bc.ProductId; break;
                    case ComponentType.PowerSupply: vm.SelectedPsuId         = bc.ProductId; break;
                    case ComponentType.Cooler:      vm.SelectedCoolerId      = bc.ProductId; break;
                }
            }
        }

        return vm;
    }
}

// ─── OrderService ────────────────────────────────────────────────────────────
public interface IOrderService
{
    Task<Order> PlaceOrderAsync(Build build, string customerName, string customerEmail,
        string customerPhone, string shippingAddress);
    Task<OrderConfirmationViewModel?> GetConfirmationAsync(string orderNumber);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepo;

    public OrderService(IOrderRepository orderRepo) => _orderRepo = orderRepo;

    public async Task<Order> PlaceOrderAsync(Build build, string customerName, string customerEmail,
        string customerPhone, string shippingAddress)
    {
        var order = new Order
        {
            OrderNumber    = $"PCB-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            CustomerName   = customerName,
            CustomerEmail  = customerEmail,
            CustomerPhone  = customerPhone,
            ShippingAddress = shippingAddress,
            TotalAmount    = build.TotalPrice,
            Status         = "Confirmed",
            Items          = build.Components.Select(bc => new OrderItem
            {
                ProductId   = bc.ProductId,
                ProductName = bc.Product.Name,
                UnitPrice   = bc.Product.Price,
                Quantity    = bc.Quantity
            }).ToList()
        };
        return await _orderRepo.CreateAsync(order);
    }

    public async Task<OrderConfirmationViewModel?> GetConfirmationAsync(string orderNumber)
    {
        var order = await _orderRepo.GetByOrderNumberAsync(orderNumber);
        if (order == null) return null;
        return new OrderConfirmationViewModel
        {
            OrderNumber   = order.OrderNumber,
            CustomerName  = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            TotalAmount   = order.TotalAmount,
            CreatedAt     = order.CreatedAt,
            Items         = order.Items.Select(i => new OrderItemViewModel
            {
                ProductName = i.ProductName,
                UnitPrice   = i.UnitPrice,
                Quantity    = i.Quantity,
                TotalPrice  = i.TotalPrice,
                Type        = i.Product?.Type ?? ComponentType.CPU
            }).ToList()
        };
    }
}
