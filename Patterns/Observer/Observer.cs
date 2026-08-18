using PCBuilder.Models;
using PCBuilder.Patterns.Adapters;
using PCBuilder.Repositories;
using Microsoft.Extensions.Logging;

namespace PCBuilder.Patterns.Observer;

// ═══════════════════════════════════════════════════════════════════════════════
// OBSERVER PATTERN
//
// Propósito: definir uma dependência um-para-muitos para que, quando um objeto
// muda de estado, todos os seus dependentes sejam notificados automaticamente.
//
// Aqui, o evento de "Pedido Realizado" (OrderPlacedEvent) é publicado pelo 
// OrderService, e múltiplos observers disparam ações independentes (e-mail, 
// estoque, fidelidade, log).
// ═══════════════════════════════════════════════════════════════════════════════

public class OrderPlacedEvent
{
    public Order Order { get; init; } = default!;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime PlacedAt { get; init; }
}

public interface IOrderObserver
{
    Task OnOrderPlacedAsync(OrderPlacedEvent evt);
}

public interface IOrderPublisher
{
    void Subscribe(IOrderObserver observer);
    void Unsubscribe(IOrderObserver observer);
    Task PublishOrderPlacedAsync(OrderPlacedEvent evt);
}

public class OrderPublisher : IOrderPublisher
{
    private readonly List<IOrderObserver> _observers = new();

    public OrderPublisher(IEnumerable<IOrderObserver> observers)
    {
        _observers.AddRange(observers);
    }

    public void Subscribe(IOrderObserver observer) => _observers.Add(observer);
    public void Unsubscribe(IOrderObserver observer) => _observers.Remove(observer);

    public async Task PublishOrderPlacedAsync(OrderPlacedEvent evt)
    {
        var tasks = _observers.Select(observer => observer.OnOrderPlacedAsync(evt));
        await Task.WhenAll(tasks);
    }
}

// ── Observers ─────────────────────────────────────────────────────────────────

public class NotificationOrderObserver : IOrderObserver
{
    private readonly INotificationDispatcher _dispatcher;

    public NotificationOrderObserver(INotificationDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task OnOrderPlacedAsync(OrderPlacedEvent evt)
    {
        await _dispatcher.NotifyOrderPlacedAsync(evt.CustomerName, evt.CustomerEmail, evt.Order.OrderNumber, evt.TotalAmount);
    }
}

public class InventoryUpdateObserver : IOrderObserver
{
    private readonly IProductRepository _productRepo;
    private readonly ILogger<InventoryUpdateObserver> _logger;

    public InventoryUpdateObserver(IProductRepository productRepo, ILogger<InventoryUpdateObserver> logger)
    {
        _productRepo = productRepo;
        _logger = logger;
    }

    public Task OnOrderPlacedAsync(OrderPlacedEvent evt)
    {
        _logger.LogInformation("[ESTOQUE] Atualizando estoque para {Count} itens do pedido {OrderNumber}", 
            evt.Order.Items.Count, evt.Order.OrderNumber);
        // Em um sistema real, aqui chamaria o repositório para decrementar estoque
        return Task.CompletedTask;
    }
}

public class LoyaltyPointsObserver : IOrderObserver
{
    private readonly ILogger<LoyaltyPointsObserver> _logger;

    public LoyaltyPointsObserver(ILogger<LoyaltyPointsObserver> logger)
    {
        _logger = logger;
    }

    public Task OnOrderPlacedAsync(OrderPlacedEvent evt)
    {
        int points = (int)(evt.TotalAmount / 10); // 1 ponto para cada 10 reais
        _logger.LogInformation("[FIDELIDADE] Cliente {Name} ganhou {Points} pontos pelo pedido {OrderNumber}.", 
            evt.CustomerName, points, evt.Order.OrderNumber);
        return Task.CompletedTask;
    }
}

public class OrderAuditLogObserver : IOrderObserver
{
    private readonly ILogger<OrderAuditLogObserver> _logger;

    public OrderAuditLogObserver(ILogger<OrderAuditLogObserver> logger)
    {
        _logger = logger;
    }

    public Task OnOrderPlacedAsync(OrderPlacedEvent evt)
    {
        _logger.LogInformation("[AUDITORIA] Pedido {OrderNumber} criado às {PlacedAt} por {Email}. Valor: {Amount:C}",
            evt.Order.OrderNumber, evt.PlacedAt, evt.CustomerEmail, evt.TotalAmount);
        return Task.CompletedTask;
    }
}
