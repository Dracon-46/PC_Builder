using PCBuilder.Models;
using PCBuilder.ViewModels;

namespace PCBuilder.Patterns.Strategy;

// ═══════════════════════════════════════════════════════════════════════════════
// STRATEGY PATTERN
//
// Propósito: definir uma família de algoritmos, encapsulá-los e torná-los intercambiáveis.
//
// Aqui, aplicamos Strategy para regras de compatibilidade. O `CompatibilityService` 
// não precisa conhecer as validações específicas, ele apenas executa uma coleção de regras.
// ═══════════════════════════════════════════════════════════════════════════════

public interface ICompatibilityRule
{
    CompatibilityError? Check(Product? cpu, Product? gpu, Product? ram, Product? storage, Product? motherboard, Product? psu, Product? cooler);
}

public class SocketCompatibilityRule : ICompatibilityRule
{
    public CompatibilityError? Check(Product? cpu, Product? gpu, Product? ram, Product? storage, Product? motherboard, Product? psu, Product? cooler)
    {
        if (cpu != null && motherboard != null)
        {
            if (!string.IsNullOrEmpty(cpu.Socket) &&
                !string.IsNullOrEmpty(motherboard.Socket) &&
                !cpu.Socket.Equals(motherboard.Socket, StringComparison.OrdinalIgnoreCase))
            {
                return new CompatibilityError
                {
                    Component = "CPU / Placa-mãe",
                    Message = $"Socket incompatível: CPU usa {cpu.Socket}, placa-mãe suporta {motherboard.Socket}.",
                    Severity = "error"
                };
            }
        }
        return null;
    }
}

public class PsuWattageCompatibilityRule : ICompatibilityRule
{
    public CompatibilityError? Check(Product? cpu, Product? gpu, Product? ram, Product? storage, Product? motherboard, Product? psu, Product? cooler)
    {
        if (psu != null)
        {
            int totalPower = 0;
            if (cpu != null) totalPower += cpu.PowerConsumption;
            if (gpu != null) totalPower += gpu.PowerConsumption;
            if (ram != null) totalPower += ram.PowerConsumption;
            if (storage != null) totalPower += storage.PowerConsumption;
            if (motherboard != null) totalPower += motherboard.PowerConsumption;
            if (cooler != null) totalPower += cooler.PowerConsumption;

            int recommended = (int)(totalPower * 1.20); // 20% headroom

            if (psu.WattageCapacity.HasValue && psu.WattageCapacity.Value < recommended)
            {
                return new CompatibilityError
                {
                    Component = "Fonte",
                    Message = $"Fonte de {psu.WattageCapacity}W pode ser insuficiente. Consumo estimado: {totalPower}W (recomendado {recommended}W com margem de segurança).",
                    Severity = psu.WattageCapacity.Value < totalPower ? "error" : "warning"
                };
            }
        }
        return null;
    }
}

public class CoolerTdpCompatibilityRule : ICompatibilityRule
{
    public CompatibilityError? Check(Product? cpu, Product? gpu, Product? ram, Product? storage, Product? motherboard, Product? psu, Product? cooler)
    {
        if (cooler != null && cpu != null)
        {
            if (cooler.TDP.HasValue && cpu.TDP.HasValue && cooler.TDP.Value < cpu.TDP.Value)
            {
                return new CompatibilityError
                {
                    Component = "Cooler",
                    Message = $"Cooler suporta até {cooler.TDP}W TDP, mas o CPU tem {cpu.TDP}W TDP. Risco de throttling térmico.",
                    Severity = "warning"
                };
            }
        }
        return null;
    }
}
