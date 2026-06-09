using Microsoft.EntityFrameworkCore;
using PCBuilder.Data;
using PCBuilder.Patterns.Adapters;
using PCBuilder.Patterns.Decorators;
using PCBuilder.Patterns.Facade;
using PCBuilder.Repositories;
using PCBuilder.Services;

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

// ── HttpClient para AwesomeAPI (cotações reais de câmbio) ─────────────────────
builder.Services.AddHttpClient("AwesomeApi", client =>
{
    client.BaseAddress = new Uri("https://economia.awesomeapi.com.br/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IBuildRepository,   BuildRepository>();
builder.Services.AddScoped<IOrderRepository,   OrderRepository>();

// ── Core Services ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<ICompatibilityService, CompatibilityService>();
builder.Services.AddScoped<IPricingService,        PricingService>();
builder.Services.AddScoped<IBuildService,          BuildService>();
builder.Services.AddScoped<IOrderService,          OrderService>();

// ── ADAPTER PATTERN ───────────────────────────────────────────────────────────
// CurrencyAdapter — agora usa HttpClient para buscar cotações reais
builder.Services.AddSingleton<ExchangeRateProvider>();
builder.Services.AddSingleton<ICurrencyAdapterFactory, CurrencyAdapterFactory>();

// ComponentSpecAdapter
builder.Services.AddScoped<IComponentSpecAdapter, ExternalSupplierAdapter>();

// NotificationAdapters
builder.Services.AddScoped<SmtpEmailService>();
builder.Services.AddScoped<FileLogService>();
builder.Services.AddScoped<INotificationAdapter, EmailNotificationAdapter>();
builder.Services.AddScoped<INotificationAdapter, LogFileNotificationAdapter>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

// ── DECORATOR PATTERN ─────────────────────────────────────────────────────────
// DiscountService orquestra a cadeia de decorators de desconto
builder.Services.AddScoped<IDiscountService, DiscountService>();

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
