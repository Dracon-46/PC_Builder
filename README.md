# PCBuilder — Sistema de Montagem de PCs Personalizados

> ASP.NET Core MVC 8 · Entity Framework Core · SQLite · Bootstrap-free design system

---

## ▶️ Como executar

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 1. Restaurar dependências
```bash
cd PCBuilder
dotnet restore
```

### 2. Aplicar migrations e seed do banco
O projeto usa **auto-migrate no startup** (`Program.cs` chama `db.Database.Migrate()` automaticamente).

Se preferir aplicar manualmente:
```bash
dotnet ef database update
```

### 3. Rodar o projeto
```bash
dotnet run
```

Acesse: **https://localhost:5001** ou **http://localhost:5000**

---

## 📁 Estrutura de pastas

```
PCBuilder/
├── PCBuilder.csproj               # Projeto + dependências NuGet
├── Program.cs                     # Entry point, DI, middleware
├── appsettings.json               # Connection string SQLite
│
├── Models/
│   └── Models.cs                  # Product, Build, BuildComponent, Order, OrderItem
│
├── Data/
│   └── AppDbContext.cs            # DbContext + SeedData (30+ componentes, 3 builds)
│
├── Migrations/
│   ├── 20240101000000_InitialCreate.cs   # Migration com schema + seed
│   └── AppDbContextModelSnapshot.cs     # Snapshot EF
│
├── ViewModels/
│   └── ViewModels.cs              # BuildViewModel, CustomizeViewModel, CheckoutViewModel, ...
│
├── Repositories/
│   └── Repositories.cs            # IProductRepository, IBuildRepository, IOrderRepository
│                                  # + implementações concretas
│
├── Services/
│   └── Services.cs                # BuildService, CompatibilityService, PricingService, OrderService
│                                  # + Mapper helper
│
├── Controllers/
│   └── Controllers.cs             # HomeController, CatalogController, BuildController, OrderController
│
├── Views/
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Shared/
│   │   ├── _Layout.cshtml         # Layout principal com header/footer/toast/theme toggle
│   │   └── _BuildCard.cshtml      # Partial reutilizável de card de build
│   ├── Home/
│   │   └── Index.cshtml           # Página inicial (hero, categorias, builds em destaque, fluxo)
│   ├── Catalog/
│   │   ├── Index.cshtml           # Catálogo completo com tabs de categorias
│   │   ├── Category.cshtml        # Lista por categoria
│   │   └── BuildDetail.cshtml     # Detalhe completo da build + botões de ação
│   ├── Build/
│   │   └── Customize.cshtml       # Personalizador completo (cards de componentes + sidebar ao vivo)
│   └── Order/
│       ├── Summary.cshtml         # Resumo do pedido com validação
│       ├── Checkout.cshtml        # Formulário de dados do cliente
│       └── Confirmation.cshtml    # Confirmação com número do pedido
│
└── wwwroot/
    ├── css/
    │   └── site.css               # Design system completo (dark mode, animações, responsivo)
    └── js/
        └── site.js                # Theme toggle, toasts, animações, validação live
```

---

## 🧩 Funcionalidades implementadas

### Catálogo
- [x] 3 categorias: Gamer Base, Gamer Pro, Workstation
- [x] Tabs de filtro por categoria
- [x] Cards de build com specs resumidas
- [x] Página de detalhe completa com todos os componentes

### Personalização
- [x] Seleção de CPU, GPU, RAM, Storage (obrigatórios)
- [x] Placa-mãe, Fonte, Cooler (opcionais)
- [x] Cards interativos com seleção visual
- [x] Sidebar com resumo atualizado em tempo real (JavaScript)
- [x] Cálculo de preço ao vivo via fetch AJAX
- [x] Validação de compatibilidade em tempo real

### Regras de compatibilidade
- [x] CPU socket vs Placa-mãe socket (ex: AM4 ≠ LGA1700 → erro)
- [x] Fonte vs consumo total + margem de 20% (erro ou aviso)
- [x] Cooler TDP vs CPU TDP (aviso de throttling)

### Fluxo do usuário (conforme diagrama)
- [x] Início → Catálogo → Categorias → Escolher categoria
- [x] Visualizar build → Personalizar ou Comprar direto
- [x] Se SIM: → Componentes → Validação → Erros → Correção → Cálculo
- [x] Revisar → Confirmar → Formulário → Finalizar

### Pedido
- [x] Resumo completo com todos os componentes
- [x] Alertas de erros e avisos de compatibilidade
- [x] Formulário com validação server-side e client-side
- [x] Máscara de telefone automática
- [x] Spinner no submit
- [x] Página de confirmação com número do pedido

### Design & UX
- [x] Dark mode por padrão com toggle (salvo em localStorage)
- [x] Tema light disponível
- [x] Animação de entrada com IntersectionObserver
- [x] Toast notifications
- [x] Diagrama de PC animado na página inicial
- [x] Totalmente responsivo (mobile, tablet, desktop)
- [x] Fonte Space Mono para elementos técnicos + DM Sans para corpo

---

## 🗄️ Dados de seed

### CPUs (6)
AMD Ryzen 5 5600X, Ryzen 7 5800X3D, Ryzen 9 7950X  
Intel Core i5-13600K, i7-13700K, i9-13900K

### GPUs (6)
AMD RX 6600 XT, RX 6700 XT, RX 7900 XTX  
NVIDIA RTX 3060 Ti, RTX 4070, RTX 4090

### RAMs (5)
16GB/32GB/64GB DDR4, 32GB/64GB DDR5

### Storage (4)
Samsung SSD 500GB, 1TB | WD NVMe 2TB | Seagate HDD 2TB

### Placas-mãe (5)
Gigabyte B550M DS3H (AM4), X570 AORUS Elite (AM4)  
ASRock X670E Taichi (AM5), MSI Z690 Tomahawk (LGA1700), MSI Z790 ACE (LGA1700)

### Fontes (4)
Corsair CV550 550W, RM750x 750W, RM850x 850W, HX1000 1000W

### Coolers (4)
Cooler Master Hyper 212, Noctua NH-D15, NZXT Kraken X63 240mm, Corsair H150i 360mm

### Builds template
1. **PC Gamer Base** — R$ 3.594 (Ryzen 5 + RX 6600 XT + 16GB DDR4 + SSD 500GB + B550M + 750W + Hyper 212)
2. **PC Gamer Pro** — R$ 9.587 (i7-13700K + RTX 4070 + 32GB DDR4 + SSD 1TB + Z690 + 850W + Kraken X63)
3. **PC Workstation** — R$ 19.687 (Ryzen 9 7950X + RTX 4090 + 64GB DDR5 + 2TB NVMe + X670E + 1000W + H150i 360mm)

---

## 🏗️ Arquitetura

```
View → Controller → Service → Repository → DbContext → SQLite
               ↓
        CompatibilityService (validação)
        PricingService (cálculo)
```

- **Repository Pattern**: `IProductRepository`, `IBuildRepository`, `IOrderRepository`
- **Service Layer**: `BuildService`, `CompatibilityService`, `PricingService`, `OrderService`
- **ViewModels**: Separados dos Models de domínio
- **Dependency Injection**: Tudo registrado em `Program.cs`
- **SOLID**: Single Responsibility em cada serviço, Dependency Inversion com interfaces

---

## 🔧 NuGet packages
```xml
Microsoft.EntityFrameworkCore 8.0.0
Microsoft.EntityFrameworkCore.Sqlite 8.0.0
Microsoft.EntityFrameworkCore.Design 8.0.0
Microsoft.EntityFrameworkCore.Tools 8.0.0
```
