using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PCBuilder.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems");

            migrationBuilder.InsertData(
                table: "Builds",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "IsCustom", "IsTemplate", "Name", "SessionId" },
                values: new object[,]
                {
                    { 1, 0, new DateTime(2026, 4, 23, 12, 16, 48, 555, DateTimeKind.Utc).AddTicks(2938), "Montagem essencial para entrar no mundo dos games com excelente custo-benefício. Roda os maiores títulos em alta qualidade.", false, true, "PC Gamer Base", null },
                    { 2, 1, new DateTime(2026, 4, 23, 12, 16, 48, 555, DateTimeKind.Utc).AddTicks(2945), "Performance de alto nível para jogos em 1440p e 4K. Overclock ready, preparado para os games mais exigentes do mercado.", false, true, "PC Gamer Pro", null },
                    { 3, 2, new DateTime(2026, 4, 23, 12, 16, 48, 555, DateTimeKind.Utc).AddTicks(2946), "Máquina de trabalho para edição de vídeo 4K, modelagem 3D, machine learning e renderização profissional.", false, true, "PC Workstation", null }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Brand", "ChipsetCompatibility", "Description", "ImageUrl", "IsAvailable", "Name", "PowerConsumption", "Price", "Socket", "TDP", "Type", "WattageCapacity" },
                values: new object[,]
                {
                    { 1, "AMD", null, "6-core, 12-thread, 3.7GHz boost 4.6GHz", null, true, "Ryzen 5 5600X", 65, 999m, "AM4", 65, 0, null },
                    { 2, "AMD", null, "8-core com 3D V-Cache, ideal para games", null, true, "Ryzen 7 5800X3D", 105, 1799m, "AM4", 105, 0, null },
                    { 3, "AMD", null, "16-core, workstation e criação de conteúdo", null, true, "Ryzen 9 7950X", 170, 3899m, "AM5", 170, 0, null },
                    { 4, "Intel", null, "14-core (6P+8E), excelente custo-benefício", null, true, "Core i5-13600K", 125, 1299m, "LGA1700", 125, 0, null },
                    { 5, "Intel", null, "16-core, alto desempenho gamer e produtividade", null, true, "Core i7-13700K", 125, 2199m, "LGA1700", 125, 0, null },
                    { 6, "Intel", null, "24-core, flagship Intel para workstation", null, true, "Core i9-13900K", 253, 3499m, "LGA1700", 253, 0, null },
                    { 10, "AMD", null, "8GB GDDR6, 1080p gaming", null, true, "RX 6600 XT", 160, 1299m, null, null, 1, null },
                    { 11, "AMD", null, "12GB GDDR6, 1440p gaming", null, true, "RX 6700 XT", 230, 1999m, null, null, 1, null },
                    { 12, "AMD", null, "24GB GDDR6, flagship 4K", null, true, "RX 7900 XTX", 355, 5499m, null, null, 1, null },
                    { 13, "NVIDIA", null, "8GB GDDR6X, 1080p/1440p gaming", null, true, "RTX 3060 Ti", 200, 1799m, null, null, 1, null },
                    { 14, "NVIDIA", null, "12GB GDDR6X, excelente 1440p", null, true, "RTX 4070", 200, 3199m, null, null, 1, null },
                    { 15, "NVIDIA", null, "24GB GDDR6X, melhor GPU para 4K e criação", null, true, "RTX 4090", 450, 9999m, null, null, 1, null },
                    { 20, "Corsair", null, "Kit 2x8GB, CL16", null, true, "16GB DDR4 3200MHz", 5, 299m, null, null, 2, null },
                    { 21, "G.Skill", null, "Kit 2x16GB, CL18, Trident Z", null, true, "32GB DDR4 3600MHz", 10, 549m, null, null, 2, null },
                    { 22, "Kingston", null, "Kit 4x16GB, para workstations", null, true, "64GB DDR4 3200MHz", 15, 999m, null, null, 2, null },
                    { 23, "G.Skill", null, "Kit 2x16GB, DDR5 alta performance", null, true, "32GB DDR5 6000MHz", 10, 849m, null, null, 2, null },
                    { 24, "Corsair", null, "Kit 2x32GB, workstation DDR5", null, true, "64GB DDR5 5600MHz", 15, 1599m, null, null, 2, null },
                    { 30, "Samsung", null, "970 EVO Plus, 3500MB/s leitura", null, true, "SSD NVMe 500GB", 5, 299m, null, null, 3, null },
                    { 31, "Samsung", null, "980 PRO, 7000MB/s PCIe 4.0", null, true, "SSD NVMe 1TB", 6, 499m, null, null, 3, null },
                    { 32, "WD", null, "Black SN850X, 7300MB/s PCIe 4.0", null, true, "SSD NVMe 2TB", 7, 899m, null, null, 3, null },
                    { 33, "Seagate", null, "BarraCuda, 7200RPM, armazenamento extra", null, true, "HDD 2TB SATA", 8, 249m, null, null, 3, null },
                    { 40, "Gigabyte", "AM4", "Micro-ATX, DDR4, PCIe 4.0", null, true, "B550M DS3H", 30, 599m, "AM4", null, 4, null },
                    { 41, "Gigabyte", "AM4", "ATX, DDR4, PCIe 4.0, WiFi", null, true, "X570 AORUS Elite", 35, 999m, "AM4", null, 4, null },
                    { 42, "ASRock", "AM5", "ATX, DDR5, PCIe 5.0, flagship AM5", null, true, "X670E Taichi", 40, 2499m, "AM5", null, 4, null },
                    { 43, "MSI", "LGA1700", "ATX, DDR4, PCIe 5.0", null, true, "Z690 Tomahawk DDR4", 35, 1099m, "LGA1700", null, 4, null },
                    { 44, "MSI", "LGA1700", "ATX, DDR5, PCIe 5.0, flagship Z790", null, true, "Z790 ACE", 40, 2199m, "LGA1700", null, 4, null },
                    { 50, "Corsair", null, "Semi-modular, proteções completas", null, true, "CV550 550W 80+ Bronze", 0, 399m, null, null, 5, 550 },
                    { 51, "Corsair", null, "Fully modular, silencioso", null, true, "RM750x 750W 80+ Gold", 0, 699m, null, null, 5, 750 },
                    { 52, "Corsair", null, "Fully modular, ideal para RTX 4080/4090", null, true, "RM850x 850W 80+ Gold", 0, 849m, null, null, 5, 850 },
                    { 53, "Corsair", null, "Modular, certificado Platinum, workstation", null, true, "HX1000 1000W 80+ Plat", 0, 1199m, null, null, 5, 1000 },
                    { 60, "Cooler Master", null, "Air cooler, suporte AM4/AM5/LGA1700", null, true, "Hyper 212 Black", 5, 199m, null, 150, 6, null },
                    { 61, "Noctua", null, "Dual tower, silencioso, top tier air", null, true, "NH-D15", 5, 599m, null, 250, 6, null },
                    { 62, "NZXT", null, "AIO 240mm, RGB, alto desempenho", null, true, "Kraken X63 240mm", 8, 699m, null, 250, 6, null },
                    { 63, "Corsair", null, "AIO 360mm, iCUE RGB, workstation/overclock", null, true, "H150i Elite 360mm", 10, 1099m, null, 350, 6, null }
                });

            migrationBuilder.InsertData(
                table: "BuildComponents",
                columns: new[] { "Id", "BuildId", "ProductId", "Quantity" },
                values: new object[,]
                {
                    { 1, 1, 1, 1 },
                    { 2, 1, 10, 1 },
                    { 3, 1, 20, 1 },
                    { 4, 1, 30, 1 },
                    { 5, 1, 40, 1 },
                    { 6, 1, 51, 1 },
                    { 7, 1, 60, 1 },
                    { 8, 2, 5, 1 },
                    { 9, 2, 14, 1 },
                    { 10, 2, 21, 1 },
                    { 11, 2, 31, 1 },
                    { 12, 2, 43, 1 },
                    { 13, 2, 52, 1 },
                    { 14, 2, 62, 1 },
                    { 15, 3, 3, 1 },
                    { 16, 3, 15, 1 },
                    { 17, 3, 24, 1 },
                    { 18, 3, 32, 1 },
                    { 19, 3, 42, 1 },
                    { 20, 3, 53, 1 },
                    { 21, 3, 63, 1 }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems");

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "BuildComponents",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "Builds",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Builds",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Builds",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
