using Microsoft.EntityFrameworkCore;
using PCBuilder.Data;
using PCBuilder.Models;

namespace PCBuilder.Repositories;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetByTypeAsync(ComponentType type);
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllAsync();
}

public interface IBuildRepository
{
    Task<IEnumerable<Build>> GetTemplatesAsync();
    Task<IEnumerable<Build>> GetTemplatesByCategoryAsync(BuildCategory category);
    Task<Build?> GetByIdWithComponentsAsync(int id);
    Task<Build?> GetBySessionIdAsync(string sessionId);
    Task<Build> CreateAsync(Build build);
    Task<Build> UpdateAsync(Build build);
    Task DeleteAsync(int id);
}

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);
    Task<Order> UpdateAsync(Order order);
    Task<Order?> GetByIdAsync(int id);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
}

// ─── Implementations ────────────────────────────────────────────────────────

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Product>> GetByTypeAsync(ComponentType type)
    {
        // OrderBy(Price) feito no cliente pois SQLite não suporta ORDER BY em decimal
        var list = await _db.Products
            .Where(p => p.Type == type && p.IsAvailable)
            .ToListAsync();
        return list.OrderBy(p => p.Price);
    }

    public async Task<Product?> GetByIdAsync(int id) =>
        await _db.Products.FindAsync(id);

    public async Task<IEnumerable<Product>> GetAllAsync() =>
        await _db.Products.Where(p => p.IsAvailable).ToListAsync();
}

public class BuildRepository : IBuildRepository
{
    private readonly AppDbContext _db;
    public BuildRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Build>> GetTemplatesAsync() =>
        await _db.Builds
            .Include(b => b.Components).ThenInclude(c => c.Product)
            .Where(b => b.IsTemplate)
            .ToListAsync();

    public async Task<IEnumerable<Build>> GetTemplatesByCategoryAsync(BuildCategory category) =>
        await _db.Builds
            .Include(b => b.Components).ThenInclude(c => c.Product)
            .Where(b => b.IsTemplate && b.Category == category)
            .ToListAsync();

    public async Task<Build?> GetByIdWithComponentsAsync(int id) =>
        await _db.Builds
            .Include(b => b.Components).ThenInclude(c => c.Product)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Build?> GetBySessionIdAsync(string sessionId) =>
        await _db.Builds
            .Include(b => b.Components).ThenInclude(c => c.Product)
            .FirstOrDefaultAsync(b => b.SessionId == sessionId);

    public async Task<Build> CreateAsync(Build build)
    {
        _db.Builds.Add(build);
        await _db.SaveChangesAsync();
        return build;
    }

    public async Task<Build> UpdateAsync(Build build)
    {
        _db.Builds.Update(build);
        await _db.SaveChangesAsync();
        return build;
    }

    public async Task DeleteAsync(int id)
    {
        var build = await _db.Builds.FindAsync(id);
        if (build != null) { _db.Builds.Remove(build); await _db.SaveChangesAsync(); }
    }
}

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;

    public async Task<Order> CreateAsync(Order order)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        _db.Orders.Update(order);
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> GetByIdAsync(int id) =>
        await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(o => o.Id == id);

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber) =>
        await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
}
