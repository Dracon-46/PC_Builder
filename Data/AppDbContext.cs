using Microsoft.EntityFrameworkCore;
using PCBuilder.Models;

namespace PCBuilder.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Build> Builds => Set<Build>();
    public DbSet<BuildComponent> BuildComponents => Set<BuildComponent>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(10,2)");
        modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(10,2)");
        modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasColumnType("decimal(10,2)");

        modelBuilder.Entity<BuildComponent>()
            .HasOne(bc => bc.Build)
            .WithMany(b => b.Components)
            .HasForeignKey(bc => bc.BuildId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BuildComponent>()
            .HasOne(bc => bc.Product)
            .WithMany(p => p.BuildComponents)
            .HasForeignKey(bc => bc.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder mb)
    {
        // ── PRODUCTS ────────────────────────────────────────────────────────────
        var products = new List<Product>
        {
            // CPUs
            new() { Id=1,  Name="Ryzen 5 5600X",         Brand="AMD",    Type=ComponentType.CPU,         Price=999m,   PowerConsumption=65,  Socket="AM4",  TDP=65,  Description="6-core, 12-thread, 3.7GHz boost 4.6GHz" },
            new() { Id=2,  Name="Ryzen 7 5800X3D",        Brand="AMD",    Type=ComponentType.CPU,         Price=1799m,  PowerConsumption=105, Socket="AM4",  TDP=105, Description="8-core com 3D V-Cache, ideal para games" },
            new() { Id=3,  Name="Ryzen 9 7950X",          Brand="AMD",    Type=ComponentType.CPU,         Price=3899m,  PowerConsumption=170, Socket="AM5",  TDP=170, Description="16-core, workstation e criação de conteúdo" },
            new() { Id=4,  Name="Core i5-13600K",         Brand="Intel",  Type=ComponentType.CPU,         Price=1299m,  PowerConsumption=125, Socket="LGA1700", TDP=125, Description="14-core (6P+8E), excelente custo-benefício" },
            new() { Id=5,  Name="Core i7-13700K",         Brand="Intel",  Type=ComponentType.CPU,         Price=2199m,  PowerConsumption=125, Socket="LGA1700", TDP=125, Description="16-core, alto desempenho gamer e produtividade" },
            new() { Id=6,  Name="Core i9-13900K",         Brand="Intel",  Type=ComponentType.CPU,         Price=3499m,  PowerConsumption=253, Socket="LGA1700", TDP=253, Description="24-core, flagship Intel para workstation" },

            // GPUs
            new() { Id=10, Name="RX 6600 XT",             Brand="AMD",    Type=ComponentType.GPU,         Price=1299m,  PowerConsumption=160, Description="8GB GDDR6, 1080p gaming" },
            new() { Id=11, Name="RX 6700 XT",             Brand="AMD",    Type=ComponentType.GPU,         Price=1999m,  PowerConsumption=230, Description="12GB GDDR6, 1440p gaming" },
            new() { Id=12, Name="RX 7900 XTX",            Brand="AMD",    Type=ComponentType.GPU,         Price=5499m,  PowerConsumption=355, Description="24GB GDDR6, flagship 4K" },
            new() { Id=13, Name="RTX 3060 Ti",            Brand="NVIDIA", Type=ComponentType.GPU,         Price=1799m,  PowerConsumption=200, Description="8GB GDDR6X, 1080p/1440p gaming" },
            new() { Id=14, Name="RTX 4070",               Brand="NVIDIA", Type=ComponentType.GPU,         Price=3199m,  PowerConsumption=200, Description="12GB GDDR6X, excelente 1440p" },
            new() { Id=15, Name="RTX 4090",               Brand="NVIDIA", Type=ComponentType.GPU,         Price=9999m,  PowerConsumption=450, Description="24GB GDDR6X, melhor GPU para 4K e criação" },

            // RAM
            new() { Id=20, Name="16GB DDR4 3200MHz",      Brand="Corsair",Type=ComponentType.RAM,         Price=299m,   PowerConsumption=5,  Description="Kit 2x8GB, CL16" },
            new() { Id=21, Name="32GB DDR4 3600MHz",      Brand="G.Skill", Type=ComponentType.RAM,        Price=549m,   PowerConsumption=10, Description="Kit 2x16GB, CL18, Trident Z" },
            new() { Id=22, Name="64GB DDR4 3200MHz",      Brand="Kingston",Type=ComponentType.RAM,        Price=999m,   PowerConsumption=15, Description="Kit 4x16GB, para workstations" },
            new() { Id=23, Name="32GB DDR5 6000MHz",      Brand="G.Skill", Type=ComponentType.RAM,        Price=849m,   PowerConsumption=10, Description="Kit 2x16GB, DDR5 alta performance" },
            new() { Id=24, Name="64GB DDR5 5600MHz",      Brand="Corsair",Type=ComponentType.RAM,         Price=1599m,  PowerConsumption=15, Description="Kit 2x32GB, workstation DDR5" },

            // Storage
            new() { Id=30, Name="SSD NVMe 500GB",         Brand="Samsung",Type=ComponentType.Storage,     Price=299m,   PowerConsumption=5,  Description="970 EVO Plus, 3500MB/s leitura" },
            new() { Id=31, Name="SSD NVMe 1TB",           Brand="Samsung",Type=ComponentType.Storage,     Price=499m,   PowerConsumption=6,  Description="980 PRO, 7000MB/s PCIe 4.0" },
            new() { Id=32, Name="SSD NVMe 2TB",           Brand="WD",     Type=ComponentType.Storage,     Price=899m,   PowerConsumption=7,  Description="Black SN850X, 7300MB/s PCIe 4.0" },
            new() { Id=33, Name="HDD 2TB SATA",           Brand="Seagate",Type=ComponentType.Storage,     Price=249m,   PowerConsumption=8,  Description="BarraCuda, 7200RPM, armazenamento extra" },

            // Motherboards
            new() { Id=40, Name="B550M DS3H",             Brand="Gigabyte",Type=ComponentType.Motherboard,Price=599m,   PowerConsumption=30, Socket="AM4",  ChipsetCompatibility="AM4", Description="Micro-ATX, DDR4, PCIe 4.0" },
            new() { Id=41, Name="X570 AORUS Elite",       Brand="Gigabyte",Type=ComponentType.Motherboard,Price=999m,   PowerConsumption=35, Socket="AM4",  ChipsetCompatibility="AM4", Description="ATX, DDR4, PCIe 4.0, WiFi" },
            new() { Id=42, Name="X670E Taichi",           Brand="ASRock", Type=ComponentType.Motherboard, Price=2499m,  PowerConsumption=40, Socket="AM5",  ChipsetCompatibility="AM5", Description="ATX, DDR5, PCIe 5.0, flagship AM5" },
            new() { Id=43, Name="Z690 Tomahawk DDR4",     Brand="MSI",    Type=ComponentType.Motherboard, Price=1099m,  PowerConsumption=35, Socket="LGA1700", ChipsetCompatibility="LGA1700", Description="ATX, DDR4, PCIe 5.0" },
            new() { Id=44, Name="Z790 ACE",               Brand="MSI",    Type=ComponentType.Motherboard, Price=2199m,  PowerConsumption=40, Socket="LGA1700", ChipsetCompatibility="LGA1700", Description="ATX, DDR5, PCIe 5.0, flagship Z790" },

            // PSU
            new() { Id=50, Name="CV550 550W 80+ Bronze",  Brand="Corsair",Type=ComponentType.PowerSupply,  Price=399m,   PowerConsumption=0,  WattageCapacity=550,  Description="Semi-modular, proteções completas" },
            new() { Id=51, Name="RM750x 750W 80+ Gold",   Brand="Corsair",Type=ComponentType.PowerSupply,  Price=699m,   PowerConsumption=0,  WattageCapacity=750,  Description="Fully modular, silencioso" },
            new() { Id=52, Name="RM850x 850W 80+ Gold",   Brand="Corsair",Type=ComponentType.PowerSupply,  Price=849m,   PowerConsumption=0,  WattageCapacity=850,  Description="Fully modular, ideal para RTX 4080/4090" },
            new() { Id=53, Name="HX1000 1000W 80+ Plat",  Brand="Corsair",Type=ComponentType.PowerSupply,  Price=1199m,  PowerConsumption=0,  WattageCapacity=1000, Description="Modular, certificado Platinum, workstation" },

            // Coolers
            new() { Id=60, Name="Hyper 212 Black",        Brand="Cooler Master",Type=ComponentType.Cooler, Price=199m,   PowerConsumption=5,  TDP=150, Description="Air cooler, suporte AM4/AM5/LGA1700" },
            new() { Id=61, Name="NH-D15",                 Brand="Noctua",Type=ComponentType.Cooler,        Price=599m,   PowerConsumption=5,  TDP=250, Description="Dual tower, silencioso, top tier air" },
            new() { Id=62, Name="Kraken X63 240mm",       Brand="NZXT",  Type=ComponentType.Cooler,        Price=699m,   PowerConsumption=8,  TDP=250, Description="AIO 240mm, RGB, alto desempenho" },
            new() { Id=63, Name="H150i Elite 360mm",      Brand="Corsair",Type=ComponentType.Cooler,       Price=1099m,  PowerConsumption=10, TDP=350, Description="AIO 360mm, iCUE RGB, workstation/overclock" },
        };
        mb.Entity<Product>().HasData(products);

        // ── TEMPLATE BUILDS ─────────────────────────────────────────────────────
        mb.Entity<Build>().HasData(
            new Build { Id=1, Name="PC Gamer Base",       Description="Montagem essencial para entrar no mundo dos games com excelente custo-benefício. Roda os maiores títulos em alta qualidade.", Category=BuildCategory.GamerBase,   IsTemplate=true },
            new Build { Id=2, Name="PC Gamer Pro",        Description="Performance de alto nível para jogos em 1440p e 4K. Overclock ready, preparado para os games mais exigentes do mercado.", Category=BuildCategory.GamerPro,    IsTemplate=true },
            new Build { Id=3, Name="PC Workstation",      Description="Máquina de trabalho para edição de vídeo 4K, modelagem 3D, machine learning e renderização profissional.", Category=BuildCategory.Workstation, IsTemplate=true }
        );

        mb.Entity<BuildComponent>().HasData(
            // Gamer Base: Ryzen 5 5600X + RX 6600 XT + 16GB DDR4 + SSD 500GB + B550M + RM750x + Hyper212
            new BuildComponent { Id=1,  BuildId=1, ProductId=1,  Quantity=1 },
            new BuildComponent { Id=2,  BuildId=1, ProductId=10, Quantity=1 },
            new BuildComponent { Id=3,  BuildId=1, ProductId=20, Quantity=1 },
            new BuildComponent { Id=4,  BuildId=1, ProductId=30, Quantity=1 },
            new BuildComponent { Id=5,  BuildId=1, ProductId=40, Quantity=1 },
            new BuildComponent { Id=6,  BuildId=1, ProductId=51, Quantity=1 },
            new BuildComponent { Id=7,  BuildId=1, ProductId=60, Quantity=1 },

            // Gamer Pro: i7-13700K + RTX 4070 + 32GB DDR4 + SSD 1TB + Z690 + RM850x + Kraken X63
            new BuildComponent { Id=8,  BuildId=2, ProductId=5,  Quantity=1 },
            new BuildComponent { Id=9,  BuildId=2, ProductId=14, Quantity=1 },
            new BuildComponent { Id=10, BuildId=2, ProductId=21, Quantity=1 },
            new BuildComponent { Id=11, BuildId=2, ProductId=31, Quantity=1 },
            new BuildComponent { Id=12, BuildId=2, ProductId=43, Quantity=1 },
            new BuildComponent { Id=13, BuildId=2, ProductId=52, Quantity=1 },
            new BuildComponent { Id=14, BuildId=2, ProductId=62, Quantity=1 },

            // Workstation: Ryzen 9 7950X + RTX 4090 + 64GB DDR5 + 2TB NVMe + X670E + HX1000 + H150i 360
            new BuildComponent { Id=15, BuildId=3, ProductId=3,  Quantity=1 },
            new BuildComponent { Id=16, BuildId=3, ProductId=15, Quantity=1 },
            new BuildComponent { Id=17, BuildId=3, ProductId=24, Quantity=1 },
            new BuildComponent { Id=18, BuildId=3, ProductId=32, Quantity=1 },
            new BuildComponent { Id=19, BuildId=3, ProductId=42, Quantity=1 },
            new BuildComponent { Id=20, BuildId=3, ProductId=53, Quantity=1 },
            new BuildComponent { Id=21, BuildId=3, ProductId=63, Quantity=1 }
        );
    }
}
