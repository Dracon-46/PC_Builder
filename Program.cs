using Microsoft.EntityFrameworkCore;
using PCBuilder.Data;
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

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IBuildRepository,   BuildRepository>();
builder.Services.AddScoped<IOrderRepository,   OrderRepository>();

// Services
builder.Services.AddScoped<ICompatibilityService, CompatibilityService>();
builder.Services.AddScoped<IPricingService,        PricingService>();
builder.Services.AddScoped<IBuildService,          BuildService>();
builder.Services.AddScoped<IOrderService,          OrderService>();

var app = builder.Build();

// Auto-migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
