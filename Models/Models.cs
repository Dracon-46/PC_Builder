using System.ComponentModel.DataAnnotations.Schema;

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

// ═══════════════════════════════════════════════════════════════════════════════
// NORMALIZAÇÃO — 3ª FORMA NORMAL
//
// Antes, Product guardava Brand / Socket / ChipsetCompatibility como texto solto,
// repetindo o mesmo valor em dezenas de linhas ("AMD", "AM4", "LGA1700"...).
// Isso viola a 3FN: são atributos que dependem de uma entidade própria, não da
// chave do produto — e abrem espaço para duplicata e erro de digitação.
//
// Agora cada um é uma tabela com chave própria e nome UNIQUE:
//   Brand    → marca do fabricante  (AMD, Intel, Corsair, ...)
//   Socket   → soquete físico       (AM4, AM5, LGA1700)
//   Chipset  → chipset da placa-mãe (B550, X570, Z690, ...) e pertence a 1 Socket
//
// O soquete da placa-mãe NÃO é mais gravado no produto: ele é determinado pelo
// chipset (Product → Chipset → Socket), eliminando a dependência transitiva.
// ═══════════════════════════════════════════════════════════════════════════════

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Socket
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;   // AM4, AM5, LGA1700
    public ICollection<Chipset> Chipsets { get; set; } = new List<Chipset>();
    public ICollection<Product> Products { get; set; } = new List<Product>();  // CPUs
}

public class Chipset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;   // B550, X570, X670E, Z690, Z790
    public int SocketId { get; set; }
    public Socket Socket { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();  // placas-mãe
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ComponentType Type { get; set; }
    public int PowerConsumption { get; set; }
    public int? TDP { get; set; }                // CPU cooling TDP
    public int? WattageCapacity { get; set; }    // PSU wattage
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;

    // ── Relacionamentos normalizados ─────────────────────────────────────────
    public int BrandId { get; set; }
    public Brand Brand { get; set; } = null!;

    public int? SocketId { get; set; }           // preenchido para CPU
    public Socket? Socket { get; set; }

    public int? ChipsetId { get; set; }          // preenchido para placa-mãe
    public Chipset? Chipset { get; set; }

    public ICollection<BuildComponent> BuildComponents { get; set; } = new List<BuildComponent>();

    /// <summary>
    /// Soquete efetivo do produto: direto (CPU) ou herdado do chipset (placa-mãe).
    /// Não é coluna no banco — é derivado, para não duplicar o dado.
    /// </summary>
    [NotMapped]
    public Socket? EffectiveSocket => Socket ?? Chipset?.Socket;
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
