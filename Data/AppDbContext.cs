using Microsoft.EntityFrameworkCore;
using PCBuilder.Models;

namespace PCBuilder.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product>        Products        => Set<Product>();
    public DbSet<Brand>          Brands          => Set<Brand>();
    public DbSet<Socket>         Sockets         => Set<Socket>();
    public DbSet<Chipset>        Chipsets        => Set<Chipset>();
    public DbSet<Build>          Builds          => Set<Build>();
    public DbSet<BuildComponent> BuildComponents => Set<BuildComponent>();
    public DbSet<Order>          Orders          => Set<Order>();
    public DbSet<OrderItem>      OrderItems      => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── 3FN: tabelas de domínio com nome único (impede duplicata) ─────────
        modelBuilder.Entity<Brand>()
            .HasIndex(b => b.Name).IsUnique();
        modelBuilder.Entity<Socket>()
            .HasIndex(s => s.Name).IsUnique();
        modelBuilder.Entity<Chipset>()
            .HasIndex(c => c.Name).IsUnique();

        // Chipset pertence a exatamente 1 Socket — é daqui que a placa-mãe
        // herda o soquete, em vez de repetir o valor na tabela de produto.
        modelBuilder.Entity<Chipset>()
            .HasOne(c => c.Socket)
            .WithMany(s => s.Chipsets)
            .HasForeignKey(c => c.SocketId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Socket)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SocketId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Chipset)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.ChipsetId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .Property(p => p.Price).HasColumnType("decimal(10,2)");
        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount).HasColumnType("decimal(10,2)");
        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice).HasColumnType("decimal(10,2)");

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
    }
}

/// <summary>
/// Popula o banco com dados iniciais caso esteja vazio.
/// Chamado no Program.cs após EnsureCreated().
/// </summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Products.Any()) return; // já seedado — não duplicar

        // ── Marcas (tabela normalizada — cada nome existe uma única vez) ──
        var brands = new[]
        {
            "AMD", "Intel", "NVIDIA", "Corsair", "G.Skill", "Kingston", "Samsung",
            "WD", "Seagate", "Gigabyte", "ASRock", "MSI", "Cooler Master", "Noctua", "NZXT"
        }.ToDictionary(n => n, n => new Brand { Name = n });
        db.Brands.AddRange(brands.Values);

        // ── Soquetes ─────────────────────────────────────────────────────
        var sockets = new[] { "AM4", "AM5", "LGA1700" }
            .ToDictionary(n => n, n => new Socket { Name = n });
        db.Sockets.AddRange(sockets.Values);

        // ── Chipsets (cada chipset pertence a um único soquete) ──────────
        var chipsets = new Dictionary<string, Chipset>
        {
            ["B550"]  = new() { Name = "B550",  Socket = sockets["AM4"] },
            ["X570"]  = new() { Name = "X570",  Socket = sockets["AM4"] },
            ["X670E"] = new() { Name = "X670E", Socket = sockets["AM5"] },
            ["Z690"]  = new() { Name = "Z690",  Socket = sockets["LGA1700"] },
            ["Z790"]  = new() { Name = "Z790",  Socket = sockets["LGA1700"] },
        };
        db.Chipsets.AddRange(chipsets.Values);
        db.SaveChanges();

        // ── Produtos ─────────────────────────────────────────────────────
        // CPU  → aponta para Socket. Placa-mãe → aponta para Chipset (e o
        // soquete vem do chipset). Os demais tipos não usam nenhum dos dois.
        var products = new List<Product>
        {
            // CPUs
            new() { Name="Ryzen 5 5600X",         Brand=brands["AMD"],           Type=ComponentType.CPU,         Price=999m,   PowerConsumption=65,  Socket=sockets["AM4"],     TDP=65,  Description="6-core, 12-thread, boost 4.6GHz" },
            new() { Name="Ryzen 7 5800X3D",       Brand=brands["AMD"],           Type=ComponentType.CPU,         Price=1799m,  PowerConsumption=105, Socket=sockets["AM4"],     TDP=105, Description="8-core 3D V-Cache, rei dos games" },
            new() { Name="Ryzen 9 7950X",         Brand=brands["AMD"],           Type=ComponentType.CPU,         Price=3899m,  PowerConsumption=170, Socket=sockets["AM5"],     TDP=170, Description="16-core, workstation e criação" },
            new() { Name="Core i5-13600K",        Brand=brands["Intel"],         Type=ComponentType.CPU,         Price=1299m,  PowerConsumption=125, Socket=sockets["LGA1700"], TDP=125, Description="14-core (6P+8E), ótimo custo-benefício" },
            new() { Name="Core i7-13700K",        Brand=brands["Intel"],         Type=ComponentType.CPU,         Price=2199m,  PowerConsumption=125, Socket=sockets["LGA1700"], TDP=125, Description="16-core, alto desempenho gamer" },
            new() { Name="Core i9-13900K",        Brand=brands["Intel"],         Type=ComponentType.CPU,         Price=3499m,  PowerConsumption=253, Socket=sockets["LGA1700"], TDP=253, Description="24-core, flagship Intel workstation" },
            // GPUs
            new() { Name="RX 6600 XT",            Brand=brands["AMD"],           Type=ComponentType.GPU,         Price=1299m,  PowerConsumption=160, Description="8GB GDDR6, 1080p gaming" },
            new() { Name="RX 6700 XT",            Brand=brands["AMD"],           Type=ComponentType.GPU,         Price=1999m,  PowerConsumption=230, Description="12GB GDDR6, 1440p gaming" },
            new() { Name="RX 7900 XTX",           Brand=brands["AMD"],           Type=ComponentType.GPU,         Price=5499m,  PowerConsumption=355, Description="24GB GDDR6, flagship 4K" },
            new() { Name="RTX 3060 Ti",           Brand=brands["NVIDIA"],        Type=ComponentType.GPU,         Price=1799m,  PowerConsumption=200, Description="8GB GDDR6X, 1080p/1440p gaming" },
            new() { Name="RTX 4070",              Brand=brands["NVIDIA"],        Type=ComponentType.GPU,         Price=3199m,  PowerConsumption=200, Description="12GB GDDR6X, excelente 1440p" },
            new() { Name="RTX 4090",              Brand=brands["NVIDIA"],        Type=ComponentType.GPU,         Price=9999m,  PowerConsumption=450, Description="24GB GDDR6X, melhor GPU do mercado" },
            // RAM
            new() { Name="16GB DDR4 3200MHz",     Brand=brands["Corsair"],       Type=ComponentType.RAM,         Price=299m,   PowerConsumption=5,  Description="Kit 2x8GB, CL16" },
            new() { Name="32GB DDR4 3600MHz",     Brand=brands["G.Skill"],       Type=ComponentType.RAM,         Price=549m,   PowerConsumption=10, Description="Kit 2x16GB, CL18, Trident Z" },
            new() { Name="64GB DDR4 3200MHz",     Brand=brands["Kingston"],      Type=ComponentType.RAM,         Price=999m,   PowerConsumption=15, Description="Kit 4x16GB, para workstations" },
            new() { Name="32GB DDR5 6000MHz",     Brand=brands["G.Skill"],       Type=ComponentType.RAM,         Price=849m,   PowerConsumption=10, Description="Kit 2x16GB, DDR5 alta performance" },
            new() { Name="64GB DDR5 5600MHz",     Brand=brands["Corsair"],       Type=ComponentType.RAM,         Price=1599m,  PowerConsumption=15, Description="Kit 2x32GB, workstation DDR5" },
            // Storage
            new() { Name="SSD NVMe 500GB",        Brand=brands["Samsung"],       Type=ComponentType.Storage,     Price=299m,   PowerConsumption=5,  Description="970 EVO Plus, 3500MB/s leitura" },
            new() { Name="SSD NVMe 1TB",          Brand=brands["Samsung"],       Type=ComponentType.Storage,     Price=499m,   PowerConsumption=6,  Description="980 PRO, 7000MB/s PCIe 4.0" },
            new() { Name="SSD NVMe 2TB",          Brand=brands["WD"],            Type=ComponentType.Storage,     Price=899m,   PowerConsumption=7,  Description="Black SN850X, 7300MB/s PCIe 4.0" },
            new() { Name="HDD 2TB SATA",          Brand=brands["Seagate"],       Type=ComponentType.Storage,     Price=249m,   PowerConsumption=8,  Description="BarraCuda, 7200RPM, armazenamento extra" },
            // Motherboards — soquete derivado do chipset
            new() { Name="B550M DS3H",            Brand=brands["Gigabyte"],      Type=ComponentType.Motherboard, Price=599m,   PowerConsumption=30, Chipset=chipsets["B550"],  Description="Micro-ATX, DDR4, PCIe 4.0" },
            new() { Name="X570 AORUS Elite",      Brand=brands["Gigabyte"],      Type=ComponentType.Motherboard, Price=999m,   PowerConsumption=35, Chipset=chipsets["X570"],  Description="ATX, DDR4, PCIe 4.0, WiFi" },
            new() { Name="X670E Taichi",          Brand=brands["ASRock"],        Type=ComponentType.Motherboard, Price=2499m,  PowerConsumption=40, Chipset=chipsets["X670E"], Description="ATX, DDR5, PCIe 5.0, flagship AM5" },
            new() { Name="Z690 Tomahawk DDR4",    Brand=brands["MSI"],           Type=ComponentType.Motherboard, Price=1099m,  PowerConsumption=35, Chipset=chipsets["Z690"],  Description="ATX, DDR4, PCIe 5.0" },
            new() { Name="Z790 ACE",              Brand=brands["MSI"],           Type=ComponentType.Motherboard, Price=2199m,  PowerConsumption=40, Chipset=chipsets["Z790"],  Description="ATX, DDR5, PCIe 5.0, flagship Z790" },
            // PSU
            new() { Name="CV550 550W 80+ Bronze", Brand=brands["Corsair"],       Type=ComponentType.PowerSupply, Price=399m,   PowerConsumption=0, WattageCapacity=550,  Description="Semi-modular, proteções completas" },
            new() { Name="RM750x 750W 80+ Gold",  Brand=brands["Corsair"],       Type=ComponentType.PowerSupply, Price=699m,   PowerConsumption=0, WattageCapacity=750,  Description="Fully modular, silencioso" },
            new() { Name="RM850x 850W 80+ Gold",  Brand=brands["Corsair"],       Type=ComponentType.PowerSupply, Price=849m,   PowerConsumption=0, WattageCapacity=850,  Description="Fully modular, ideal para RTX 4080/4090" },
            new() { Name="HX1000 1000W 80+ Plat", Brand=brands["Corsair"],       Type=ComponentType.PowerSupply, Price=1199m,  PowerConsumption=0, WattageCapacity=1000, Description="Modular, certificado Platinum" },
            // Coolers
            new() { Name="Hyper 212 Black",       Brand=brands["Cooler Master"], Type=ComponentType.Cooler,      Price=199m,   PowerConsumption=5,  TDP=150, Description="Air cooler, suporte AM4/AM5/LGA1700" },
            new() { Name="NH-D15",                Brand=brands["Noctua"],        Type=ComponentType.Cooler,      Price=599m,   PowerConsumption=5,  TDP=250, Description="Dual tower, silencioso, top tier air" },
            new() { Name="Kraken X63 240mm",      Brand=brands["NZXT"],          Type=ComponentType.Cooler,      Price=699m,   PowerConsumption=8,  TDP=250, Description="AIO 240mm, RGB, alto desempenho" },
            new() { Name="H150i Elite 360mm",     Brand=brands["Corsair"],       Type=ComponentType.Cooler,      Price=1099m,  PowerConsumption=10, TDP=350, Description="AIO 360mm, iCUE RGB, overclock" },
        };

        db.Products.AddRange(products);
        db.SaveChanges();

        // Helper local para buscar produto pelo nome após o SaveChanges
        Product P(string name) => db.Products.First(p => p.Name == name);

        // ── Builds template ──────────────────────────────────────────────
        db.Builds.AddRange(
            new Build
            {
                Name        = "PC Gamer Base",
                Description = "Montagem essencial para entrar no mundo dos games com excelente custo-benefício. Roda os maiores títulos em alta qualidade.",
                Category    = BuildCategory.GamerBase,
                IsTemplate  = true,
                Components  =
                [
                    new() { Product = P("Ryzen 5 5600X"),       Quantity = 1 },
                    new() { Product = P("RX 6600 XT"),          Quantity = 1 },
                    new() { Product = P("16GB DDR4 3200MHz"),   Quantity = 1 },
                    new() { Product = P("SSD NVMe 500GB"),      Quantity = 1 },
                    new() { Product = P("B550M DS3H"),          Quantity = 1 },
                    new() { Product = P("RM750x 750W 80+ Gold"),Quantity = 1 },
                    new() { Product = P("Hyper 212 Black"),     Quantity = 1 },
                ]
            },
            new Build
            {
                Name        = "PC Gamer Pro",
                Description = "Performance de alto nível para jogos em 1440p e 4K. Overclock ready, preparado para os games mais exigentes do mercado.",
                Category    = BuildCategory.GamerPro,
                IsTemplate  = true,
                Components  =
                [
                    new() { Product = P("Core i7-13700K"),       Quantity = 1 },
                    new() { Product = P("RTX 4070"),             Quantity = 1 },
                    new() { Product = P("32GB DDR4 3600MHz"),    Quantity = 1 },
                    new() { Product = P("SSD NVMe 1TB"),         Quantity = 1 },
                    new() { Product = P("Z690 Tomahawk DDR4"),   Quantity = 1 },
                    new() { Product = P("RM850x 850W 80+ Gold"), Quantity = 1 },
                    new() { Product = P("Kraken X63 240mm"),     Quantity = 1 },
                ]
            },
            new Build
            {
                Name        = "PC Workstation",
                Description = "Máquina de trabalho para edição de vídeo 4K, modelagem 3D, machine learning e renderização profissional.",
                Category    = BuildCategory.Workstation,
                IsTemplate  = true,
                Components  =
                [
                    new() { Product = P("Ryzen 9 7950X"),        Quantity = 1 },
                    new() { Product = P("RTX 4090"),             Quantity = 1 },
                    new() { Product = P("64GB DDR5 5600MHz"),    Quantity = 1 },
                    new() { Product = P("SSD NVMe 2TB"),         Quantity = 1 },
                    new() { Product = P("X670E Taichi"),         Quantity = 1 },
                    new() { Product = P("HX1000 1000W 80+ Plat"),Quantity = 1 },
                    new() { Product = P("H150i Elite 360mm"),    Quantity = 1 },
                ]
            }
        );

        db.SaveChanges();
    }
}
