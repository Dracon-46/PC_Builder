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
            // Com o banco normalizado, o soquete é uma entidade: a CPU aponta direto
            // para ele e a placa-mãe herda do chipset. A comparação passa a ser por
            // chave (Id), não por string — sem risco de divergência de digitação.
            var cpuSocket = cpu.EffectiveSocket;
            var mbSocket  = motherboard.EffectiveSocket;

            if (cpuSocket != null && mbSocket != null && cpuSocket.Id != mbSocket.Id)
            {
                return new CompatibilityError
                {
                    Component = "CPU / Placa-mãe",
                    Message = $"Socket incompatível: CPU usa {cpuSocket.Name}, placa-mãe suporta {mbSocket.Name}.",
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
