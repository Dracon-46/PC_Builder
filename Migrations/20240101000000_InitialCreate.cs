using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace PCBuilder.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    OrderNumber = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerEmail = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerPhone = table.Column<string>(type: "TEXT", nullable: false),
                    ShippingAddress = table.Column<string>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Orders", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Brand = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    PowerConsumption = table.Column<int>(type: "INTEGER", nullable: false),
                    Socket = table.Column<string>(type: "TEXT", nullable: true),
                    ChipsetCompatibility = table.Column<string>(type: "TEXT", nullable: true),
                    TDP = table.Column<int>(type: "INTEGER", nullable: true),
                    WattageCapacity = table.Column<int>(type: "INTEGER", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Products", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Builds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    IsTemplate = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCustom = table.Column<bool>(type: "INTEGER", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Builds", x => x.Id));

            migrationBuilder.CreateTable(
                name: "BuildComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    BuildId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildComponents", x => x.Id);
                    table.ForeignKey(name: "FK_BuildComponents_Builds_BuildId", column: x => x.BuildId, principalTable: "Builds", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_BuildComponents_Products_ProductId", column: x => x.ProductId, principalTable: "Products", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(name: "FK_OrderItems_Orders_OrderId", column: x => x.OrderId, principalTable: "Orders", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_OrderItems_Products_ProductId", column: x => x.ProductId, principalTable: "Products", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_BuildComponents_BuildId",   table: "BuildComponents", column: "BuildId");
            migrationBuilder.CreateIndex(name: "IX_BuildComponents_ProductId", table: "BuildComponents", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_OrderItems_OrderId",        table: "OrderItems",      column: "OrderId");
            migrationBuilder.CreateIndex(name: "IX_OrderItems_ProductId",      table: "OrderItems",      column: "ProductId");

            // ── SEED DATA ────────────────────────────────────────────────────────
            // Products
            migrationBuilder.InsertData("Products", new[]{"Id","Name","Brand","Description","Price","Type","PowerConsumption","Socket","ChipsetCompatibility","TDP","WattageCapacity","ImageUrl","IsAvailable"},new object[,]{
                {1,"Ryzen 5 5600X","AMD","6-core, 12-thread, 3.7GHz boost 4.6GHz",999m,0,65,"AM4",null,65,null,null,true},
                {2,"Ryzen 7 5800X3D","AMD","8-core com 3D V-Cache, ideal para games",1799m,0,105,"AM4",null,105,null,null,true},
                {3,"Ryzen 9 7950X","AMD","16-core, workstation e criação de conteúdo",3899m,0,170,"AM5",null,170,null,null,true},
                {4,"Core i5-13600K","Intel","14-core (6P+8E), excelente custo-benefício",1299m,0,125,"LGA1700",null,125,null,null,true},
                {5,"Core i7-13700K","Intel","16-core, alto desempenho gamer e produtividade",2199m,0,125,"LGA1700",null,125,null,null,true},
                {6,"Core i9-13900K","Intel","24-core, flagship Intel para workstation",3499m,0,253,"LGA1700",null,253,null,null,true},
                {10,"RX 6600 XT","AMD","8GB GDDR6, 1080p gaming",1299m,1,160,null,null,null,null,null,true},
                {11,"RX 6700 XT","AMD","12GB GDDR6, 1440p gaming",1999m,1,230,null,null,null,null,null,true},
                {12,"RX 7900 XTX","AMD","24GB GDDR6, flagship 4K",5499m,1,355,null,null,null,null,null,true},
                {13,"RTX 3060 Ti","NVIDIA","8GB GDDR6X, 1080p/1440p gaming",1799m,1,200,null,null,null,null,null,true},
                {14,"RTX 4070","NVIDIA","12GB GDDR6X, excelente 1440p",3199m,1,200,null,null,null,null,null,true},
                {15,"RTX 4090","NVIDIA","24GB GDDR6X, melhor GPU para 4K e criação",9999m,1,450,null,null,null,null,null,true},
                {20,"16GB DDR4 3200MHz","Corsair","Kit 2x8GB, CL16",299m,2,5,null,null,null,null,null,true},
                {21,"32GB DDR4 3600MHz","G.Skill","Kit 2x16GB, CL18, Trident Z",549m,2,10,null,null,null,null,null,true},
                {22,"64GB DDR4 3200MHz","Kingston","Kit 4x16GB, para workstations",999m,2,15,null,null,null,null,null,true},
                {23,"32GB DDR5 6000MHz","G.Skill","Kit 2x16GB, DDR5 alta performance",849m,2,10,null,null,null,null,null,true},
                {24,"64GB DDR5 5600MHz","Corsair","Kit 2x32GB, workstation DDR5",1599m,2,15,null,null,null,null,null,true},
                {30,"SSD NVMe 500GB","Samsung","970 EVO Plus, 3500MB/s leitura",299m,3,5,null,null,null,null,null,true},
                {31,"SSD NVMe 1TB","Samsung","980 PRO, 7000MB/s PCIe 4.0",499m,3,6,null,null,null,null,null,true},
                {32,"SSD NVMe 2TB","WD","Black SN850X, 7300MB/s PCIe 4.0",899m,3,7,null,null,null,null,null,true},
                {33,"HDD 2TB SATA","Seagate","BarraCuda, 7200RPM, armazenamento extra",249m,3,8,null,null,null,null,null,true},
                {40,"B550M DS3H","Gigabyte","Micro-ATX, DDR4, PCIe 4.0",599m,4,30,"AM4","AM4",null,null,null,true},
                {41,"X570 AORUS Elite","Gigabyte","ATX, DDR4, PCIe 4.0, WiFi",999m,4,35,"AM4","AM4",null,null,null,true},
                {42,"X670E Taichi","ASRock","ATX, DDR5, PCIe 5.0, flagship AM5",2499m,4,40,"AM5","AM5",null,null,null,true},
                {43,"Z690 Tomahawk DDR4","MSI","ATX, DDR4, PCIe 5.0",1099m,4,35,"LGA1700","LGA1700",null,null,null,true},
                {44,"Z790 ACE","MSI","ATX, DDR5, PCIe 5.0, flagship Z790",2199m,4,40,"LGA1700","LGA1700",null,null,null,true},
                {50,"CV550 550W 80+ Bronze","Corsair","Semi-modular, proteções completas",399m,5,0,null,null,null,550,null,true},
                {51,"RM750x 750W 80+ Gold","Corsair","Fully modular, silencioso",699m,5,0,null,null,null,750,null,true},
                {52,"RM850x 850W 80+ Gold","Corsair","Fully modular, ideal para RTX 4080/4090",849m,5,0,null,null,null,850,null,true},
                {53,"HX1000 1000W 80+ Plat","Corsair","Modular, certificado Platinum, workstation",1199m,5,0,null,null,null,1000,null,true},
                {60,"Hyper 212 Black","Cooler Master","Air cooler, suporte AM4/AM5/LGA1700",199m,6,5,null,null,150,null,null,true},
                {61,"NH-D15","Noctua","Dual tower, silencioso, top tier air",599m,6,5,null,null,250,null,null,true},
                {62,"Kraken X63 240mm","NZXT","AIO 240mm, RGB, alto desempenho",699m,6,8,null,null,250,null,null,true},
                {63,"H150i Elite 360mm","Corsair","AIO 360mm, iCUE RGB, workstation/overclock",1099m,6,10,null,null,350,null,null,true}
            });

            migrationBuilder.InsertData("Builds",
                new[]{"Id","Name","Description","Category","IsTemplate","IsCustom","SessionId","CreatedAt"},
                new object[,]{
                    {1,"PC Gamer Base","Montagem essencial para entrar no mundo dos games com excelente custo-benefício. Roda os maiores títulos em alta qualidade.",0,true,false,null,new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc)},
                    {2,"PC Gamer Pro","Performance de alto nível para jogos em 1440p e 4K. Overclock ready, preparado para os games mais exigentes do mercado.",1,true,false,null,new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc)},
                    {3,"PC Workstation","Máquina de trabalho para edição de vídeo 4K, modelagem 3D, machine learning e renderização profissional.",2,true,false,null,new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc)}
                });

            migrationBuilder.InsertData("BuildComponents",
                new[]{"Id","BuildId","ProductId","Quantity"},
                new object[,]{
                    {1,1,1,1},{2,1,10,1},{3,1,20,1},{4,1,30,1},{5,1,40,1},{6,1,51,1},{7,1,60,1},
                    {8,2,5,1},{9,2,14,1},{10,2,21,1},{11,2,31,1},{12,2,43,1},{13,2,52,1},{14,2,62,1},
                    {15,3,3,1},{16,3,15,1},{17,3,24,1},{18,3,32,1},{19,3,42,1},{20,3,53,1},{21,3,63,1}
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BuildComponents");
            migrationBuilder.DropTable(name: "OrderItems");
            migrationBuilder.DropTable(name: "Builds");
            migrationBuilder.DropTable(name: "Orders");
            migrationBuilder.DropTable(name: "Products");
        }
    }
}
