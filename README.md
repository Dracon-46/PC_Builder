# PCBuilder — Padrões de Projeto: Facade + Adapter

> ASP.NET Core MVC 8 · Entity Framework Core · SQLite · Padrões GoF aplicados

---

## ▶️ Como rodar

```bash
# 1. Se tiver um pcbuilder.db antigo, delete-o:
del pcbuilder.db   # Windows
rm pcbuilder.db    # Linux/Mac

# 2. Rodar:
dotnet run
```

Banco criado e populado automaticamente. Acesse: http://localhost:5000

---

## 🏗️ Arquitetura com Padrões de Projeto

```
┌─────────────────────────────────────────────────────────────┐
│                        CONTROLLERS                          │
│  HomeController  CatalogController  BuildController         │
│  OrderController                                            │
│           │  Injetam apenas IPCBuilderFacade                │
└───────────┼─────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────────┐
│              FACADE  —  PCBuilderFacade                     │
│                                                             │
│  Métodos de alto nível:                                     │
│   GetBuildDetailAsync()     → chama BuildService +         │
│                               CurrencyAdapter +             │
│                               ComponentSpecAdapter          │
│   SaveCustomizationAsync()  → chama ProductRepo +          │
│                               CompatibilityService +        │
│                               PricingService + BuildService │
│   PlaceOrderAsync()         → chama OrderService +         │
│                               NotificationDispatcher        │
│   GetLivePriceAsync()       → chama PricingService +       │
│                               CompatibilityService +        │
│                               CurrencyAdapter               │
└──────┬──────────────────────────────────────────────────────┘
       │  Orquestra
       ▼
┌─────────────────────────────────────────────────────────────┐
│                     ADAPTERS                                │
│                                                             │
│  ICurrencyAdapter                                           │
│   ├─ UsdCurrencyAdapter  (BRL → USD via ExchangeRateProvider)│
│   └─ EurCurrencyAdapter  (BRL → EUR via ExchangeRateProvider)│
│                                                             │
│  IComponentSpecAdapter                                      │
│   └─ ExternalSupplierAdapter                               │
│       Product  ←→  ExternalSupplierComponentDto            │
│       Adiciona: SupplierSku, WarrantyMonths,               │
│                 AvailabilityStatus                          │
│                                                             │
│  INotificationAdapter                                       │
│   ├─ EmailNotificationAdapter  (adapta SmtpEmailService)   │
│   └─ LogFileNotificationAdapter (adapta FileLogService)    │
│       NotificationDispatcher envia para todos os canais    │
└─────────────────────────────────────────────────────────────┘
       │  Usa
       ▼
┌─────────────────────────────────────────────────────────────┐
│                 CORE SERVICES / REPOSITORIES                │
│  BuildService · CompatibilityService · PricingService       │
│  OrderService                                               │
│  ProductRepository · BuildRepository · OrderRepository      │
│  AppDbContext (SQLite)                                      │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎭 Padrão Facade

**Arquivo:** `Patterns/Facade/PCBuilderFacade.cs`

### Por que Facade aqui?

Antes de aplicar o padrão, o `BuildController` precisava injetar e coordenar:

```csharp
// ANTES — Controller com 5 dependências e lógica de orquestração
public BuildController(
    IBuildService buildService,
    ICompatibilityService compat,
    IPricingService pricing,
    IProductRepository productRepo,
    IBuildRepository buildRepo) { ... }
```

Depois do Facade, **todos os controllers injetam apenas uma dependência**:

```csharp
// DEPOIS — Controller limpo
public BuildController(IPCBuilderFacade facade) => _facade = facade;
```

### Interface da Facade

```csharp
public interface IPCBuilderFacade
{
    Task<BuildDetailViewModel?> GetBuildDetailAsync(int id, string currency);
    Task<SaveCustomizationResult> SaveCustomizationAsync(string session, ...);
    Task<LivePriceResult> GetLivePriceAsync(..., string currency);
    Task<Order> PlaceOrderAsync(PlaceOrderRequest request);  // + notifica
    Task<IEnumerable<EnrichedProductViewModel>> GetEnrichedProductsAsync(ComponentType type);
    IEnumerable<string> GetSupportedCurrencies();
    // ...
}
```

---

## 🔌 Padrão Adapter (3 implementações)

**Arquivo:** `Patterns/Adapters/Adapters.cs`

### 1. ICurrencyAdapter — Conversão de Moeda

Adapta a interface do `ExchangeRateProvider` (API externa simulada) para o que o sistema espera (`ICurrencyAdapter`).

```
ExchangeRateProvider  →  UsdCurrencyAdapter  →  ICurrencyAdapter
(tem GetUsdRate())        (adapta o método)       (tem Format(decimal))

ExchangeRateProvider  →  EurCurrencyAdapter  →  ICurrencyAdapter
```

**Onde aparece na UI:** página de detalhe da build — botões BRL / USD / EUR.
O `CurrencyAdapterFactory` seleciona o adapter correto pelo código.

### 2. IComponentSpecAdapter — Fornecedor Externo

Adapta `Product` (modelo interno) ↔ `ExternalSupplierComponentDto` (formato do fornecedor externo).

```
Product  ──ToExternalDto()──►  ExternalSupplierComponentDto
         ◄──FromExternalDto()─   (SupplierSku, WarrantyMonths,
                                  AvailabilityCode, PriceUsd...)

ProductViewModel  ──Enrich()──►  EnrichedProductViewModel
                                  (todos os campos originais +
                                   dados do fornecedor)
```

**Onde aparece na UI:** página de detalhe da build — chips de SKU, garantia e disponibilidade.

### 3. INotificationAdapter — Multi-canal

Adapta diferentes serviços de envio (`SmtpEmailService`, `FileLogService`) para a mesma interface `INotificationAdapter`. O `NotificationDispatcher` usa `IEnumerable<INotificationAdapter>` e despacha para todos.

```
SmtpEmailService   →  EmailNotificationAdapter   →  INotificationAdapter
FileLogService     →  LogFileNotificationAdapter  →  INotificationAdapter
                        ▲
                  NotificationDispatcher (usa IEnumerable<INotificationAdapter>)
```

**Quando dispara:** ao confirmar um pedido, a Facade chama `NotificationDispatcher.NotifyOrderPlacedAsync()` automaticamente.

---

## 📁 Estrutura de pastas

```
PCBuilder/
├── Controllers/
│   └── Controllers.cs         ← usam apenas IPCBuilderFacade
├── Data/
│   └── AppDbContext.cs        ← DbContext + DbSeeder
├── Models/
│   └── Models.cs              ← Product, Build, Order, etc.
├── Patterns/
│   ├── Adapters/
│   │   └── Adapters.cs        ← ICurrencyAdapter (USD/EUR)
│   │                             IComponentSpecAdapter (fornecedor externo)
│   │                             INotificationAdapter (Email/Log)
│   │                             NotificationDispatcher
│   └── Facade/
│       └── PCBuilderFacade.cs ← IPCBuilderFacade + PCBuilderFacade
│                                 SaveCustomizationResult, LivePriceResult
│                                 PlaceOrderRequest, BuildDetailViewModel
├── Repositories/
│   └── Repositories.cs
├── Services/
│   └── Services.cs
├── ViewModels/
│   └── ViewModels.cs          ← + EnrichedProductViewModel, BuildDetailViewModel
├── Views/
│   ├── Catalog/BuildDetail    ← mostra currency selector + dados do fornecedor
│   └── ...
└── wwwroot/
    ├── css/site.css           ← estilos para currency selector, availability chips
    └── js/site.js
```

---

## ✨ Funcionalidades novas (via padrões)

| Feature | Padrão | Onde ver |
|---|---|---|
| Preço em USD / EUR | Adapter (Currency) | /Catalog/BuildDetail/{id}?currency=USD |
| SKU do fornecedor, garantia, disponibilidade | Adapter (ComponentSpec) | /Catalog/BuildDetail/{id} |
| Notificação automática ao confirmar pedido | Adapter (Notification) | Log do console ao finalizar |
| Controller com 1 dependência só | Facade | Controllers/Controllers.cs |
| Orquestração centralizada | Facade | Patterns/Facade/PCBuilderFacade.cs |
| API JSON de componentes enriquecidos | Facade + Adapter | GET /Build/Components?type=CPU |

