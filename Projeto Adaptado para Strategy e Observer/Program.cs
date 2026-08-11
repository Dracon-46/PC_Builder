using Microsoft.EntityFrameworkCore;
using PCBuilder.Data;
using PCBuilder.Patterns.Adapters;
using PCBuilder.Patterns.Facade;
using PCBuilder.Repositories;
using PCBuilder.Services;
using PCBuilder.Patterns.Strategy;
using PCBuilder.Patterns.Observer;
using PCBuilder.Patterns.Decorator;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(opts =>
{
    opts.IdleTimeout = TimeSpan.FromHours(2);
    opts.Cookie.HttpOnly = true;
    opts.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=pcbuilder.db"));

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IBuildRepository,   BuildRepository>();
builder.Services.AddScoped<IOrderRepository,   OrderRepository>();

// ── STRATEGY PATTERN ──────────────────────────────────────────────────────────
builder.Services.AddScoped<ICompatibilityRule, SocketCompatibilityRule>();
builder.Services.AddScoped<ICompatibilityRule, PsuWattageCompatibilityRule>();
builder.Services.AddScoped<ICompatibilityRule, CoolerTdpCompatibilityRule>();

// ── OBSERVER PATTERN ──────────────────────────────────────────────────────────
builder.Services.AddScoped<IOrderObserver, NotificationOrderObserver>();
builder.Services.AddScoped<IOrderObserver, InventoryUpdateObserver>();
builder.Services.AddScoped<IOrderObserver, LoyaltyPointsObserver>();
builder.Services.AddScoped<IOrderObserver, OrderAuditLogObserver>();
builder.Services.AddScoped<IOrderPublisher, OrderPublisher>();

// ── DECORATOR PATTERN ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IDiscountService, DiscountService>();

// ── Core Services ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<ICompatibilityService, CompatibilityService>();
builder.Services.AddScoped<IPricingService,        PricingService>();
builder.Services.AddScoped<IBuildService,          BuildService>();
builder.Services.AddScoped<IOrderService,          OrderService>();

// ── ADAPTER PATTERN ───────────────────────────────────────────────────────────
// CurrencyAdapter
builder.Services.AddSingleton<ExchangeRateProvider>();
builder.Services.AddSingleton<ICurrencyAdapterFactory, CurrencyAdapterFactory>();

// ComponentSpecAdapter
builder.Services.AddScoped<IComponentSpecAdapter, ExternalSupplierAdapter>();

// NotificationAdapters — registra múltiplos canais, dispatcher injeta IEnumerable<>
builder.Services.AddScoped<SmtpEmailService>();
builder.Services.AddScoped<FileLogService>();
builder.Services.AddScoped<INotificationAdapter, EmailNotificationAdapter>();
builder.Services.AddScoped<INotificationAdapter, LogFileNotificationAdapter>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

// ── FACADE PATTERN ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPCBuilderFacade, PCBuilderFacade>();

var app = builder.Build();

// Cria o schema e popula o banco automaticamente no primeiro run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(
                "<h1 style='font-family:sans-serif'>Erro interno.</h1><a href='/'>← Voltar ao início</a>");
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
