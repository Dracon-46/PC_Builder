using PCBuilder.Models;

namespace PCBuilder.Patterns.State;

// ═══════════════════════════════════════════════════════════════════════════════
// STATE PATTERN
//
// Propósito: permitir que um objeto altere seu comportamento quando seu estado
// interno muda — o objeto parece mudar de classe.
//
// Aqui, o ciclo de vida de um Order (pedido) é modelado como uma máquina de
// estados: Pending → Confirmed → Processing → Shipped → Delivered, com Cancelled
// como estado terminal alternativo. Cada IOrderState sabe para qual estado pode
// avançar/cancelar e expõe os dados que a apresentação precisa (rótulo, cor do
// badge, próxima ação). O OrderService e os Controllers não precisam mais
// espalhar "if status == ..." pelo código — eles só chamam Advance()/Cancel().
// ═══════════════════════════════════════════════════════════════════════════════

public interface IOrderState
{
    string Key { get; }              // valor persistido em Order.Status
    string Label { get; }            // rótulo exibido ao cliente
    string BadgeCssClass { get; }    // classe CSS do badge de status
    string? NextActionLabel { get; } // rótulo do botão de avanço (null = estado terminal)
    bool CanCancel { get; }

    IOrderState Advance();
    IOrderState Cancel();
}

public abstract class OrderStateBase : IOrderState
{
    public abstract string Key { get; }
    public abstract string Label { get; }
    public abstract string BadgeCssClass { get; }
    public abstract string? NextActionLabel { get; }
    public virtual bool CanCancel => true;

    public virtual IOrderState Advance() =>
        throw new InvalidOperationException($"Pedido em '{Label}' não pode avançar de status.");

    public virtual IOrderState Cancel() =>
        CanCancel
            ? new CancelledState()
            : throw new InvalidOperationException($"Pedido em '{Label}' não pode mais ser cancelado.");
}

// ── Estados concretos ─────────────────────────────────────────────────────────

public sealed class PendingState : OrderStateBase
{
    public override string Key => "Pending";
    public override string Label => "Pendente";
    public override string BadgeCssClass => "status-pending";
    public override string? NextActionLabel => "Confirmar pagamento";
    public override IOrderState Advance() => new ConfirmedState();
}

public sealed class ConfirmedState : OrderStateBase
{
    public override string Key => "Confirmed";
    public override string Label => "Confirmado";
    public override string BadgeCssClass => "status-confirmed";
    public override string? NextActionLabel => "Iniciar preparo";
    public override IOrderState Advance() => new ProcessingState();
}

public sealed class ProcessingState : OrderStateBase
{
    public override string Key => "Processing";
    public override string Label => "Em preparação";
    public override string BadgeCssClass => "status-processing";
    public override string? NextActionLabel => "Enviar pedido";
    public override IOrderState Advance() => new ShippedState();
}

public sealed class ShippedState : OrderStateBase
{
    public override string Key => "Shipped";
    public override string Label => "Enviado";
    public override string BadgeCssClass => "status-shipped";
    public override string? NextActionLabel => "Confirmar entrega";
    public override bool CanCancel => false; // já saiu para entrega — não cancela mais
    public override IOrderState Advance() => new DeliveredState();
}

public sealed class DeliveredState : OrderStateBase
{
    public override string Key => "Delivered";
    public override string Label => "Entregue";
    public override string BadgeCssClass => "status-delivered";
    public override string? NextActionLabel => null; // estado terminal
    public override bool CanCancel => false;
}

public sealed class CancelledState : OrderStateBase
{
    public override string Key => "Cancelled";
    public override string Label => "Cancelado";
    public override string BadgeCssClass => "status-cancelled";
    public override string? NextActionLabel => null; // estado terminal
    public override bool CanCancel => false;
}

// ── Fábrica de estados ────────────────────────────────────────────────────────

public static class OrderStateFactory
{
    public static IOrderState Initial => new PendingState();

    public static IOrderState FromKey(string key) => key switch
    {
        "Pending"    => new PendingState(),
        "Confirmed"  => new ConfirmedState(),
        "Processing" => new ProcessingState(),
        "Shipped"    => new ShippedState(),
        "Delivered"  => new DeliveredState(),
        "Cancelled"  => new CancelledState(),
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Status de pedido desconhecido.")
    };
}

// ── Contexto — liga o Order ao seu estado atual ───────────────────────────────

public class OrderStateContext
{
    private readonly Order _order;

    public OrderStateContext(Order order) => _order = order;

    public IOrderState CurrentState => OrderStateFactory.FromKey(_order.Status);

    public IOrderState Advance()
    {
        var next = CurrentState.Advance();
        _order.Status = next.Key;
        return next;
    }

    public IOrderState Cancel()
    {
        var next = CurrentState.Cancel();
        _order.Status = next.Key;
        return next;
    }
}
