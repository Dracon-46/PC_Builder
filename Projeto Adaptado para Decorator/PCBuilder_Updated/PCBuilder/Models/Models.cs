namespace PCBuilder.Models;

public enum ComponentType
{
    CPU,
    GPU,
    RAM,
    Storage,
    Motherboard,
    PowerSupply,
    Cooler
}

public enum BuildCategory
{
    GamerBase,
    GamerPro,
    Workstation
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ComponentType Type { get; set; }
    public int PowerConsumption { get; set; }
    public string? Socket { get; set; }          // CPU/Motherboard
    public string? ChipsetCompatibility { get; set; } // Motherboard supports these CPU generations
    public int? TDP { get; set; }                // CPU cooling TDP
    public int? WattageCapacity { get; set; }    // PSU wattage
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    public ICollection<BuildComponent> BuildComponents { get; set; } = new List<BuildComponent>();
}

public class Build
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BuildCategory Category { get; set; }
    public bool IsTemplate { get; set; } = true;
    public bool IsCustom { get; set; } = false;
    public string? SessionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<BuildComponent> Components { get; set; } = new List<BuildComponent>();

    public decimal TotalPrice => Components.Sum(c => c.Product?.Price ?? 0);
    public int TotalPower => Components.Sum(c => c.Product?.PowerConsumption ?? 0);
}

public class BuildComponent
{
    public int Id { get; set; }
    public int BuildId { get; set; }
    public Build Build { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; } = 1;
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalPrice => UnitPrice * Quantity;
}
