using Microsoft.EntityFrameworkCore;
using PTRON.Models;

namespace PTRON.Data;

/// <summary>
/// Idempotent sample data for a fresh database (tipos + insumos only).
/// Skips seeding when any tipo already exists.
/// </summary>
public static class SeedData
{
    public static async Task EnsureSeededAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();

        if (await db.TiposInsumo.AnyAsync())
        {
            return;
        }

        var tipos = new[]
        {
            new TipoInsumo { Nome = "Resistor" },
            new TipoInsumo { Nome = "Capacitor" },
            new TipoInsumo { Nome = "Diodo" },
            new TipoInsumo { Nome = "Transistor" },
            new TipoInsumo { Nome = "CI" },
            new TipoInsumo { Nome = "Válvula" },
        };

        db.TiposInsumo.AddRange(tipos);
        await db.SaveChangesAsync();

        // Reload to get generated Ids.
        var resistor = await db.TiposInsumo.FirstAsync(t => t.Nome == "Resistor");
        var capacitor = await db.TiposInsumo.FirstAsync(t => t.Nome == "Capacitor");
        var diodo = await db.TiposInsumo.FirstAsync(t => t.Nome == "Diodo");
        var transistor = await db.TiposInsumo.FirstAsync(t => t.Nome == "Transistor");
        var ci = await db.TiposInsumo.FirstAsync(t => t.Nome == "CI");

        db.Insumos.AddRange(
            new Insumo
            {
                TipoInsumoId = resistor.Id,
                Nome = "Resistor 1kΩ 1/4W",
                Valor = "1kΩ",
                Potencia = "1/4W",
                Saldo = 0,
                CustoUnitario = 0
            },
            new Insumo
            {
                TipoInsumoId = resistor.Id,
                Nome = "Resistor 10kΩ 1/4W",
                Valor = "10kΩ",
                Potencia = "1/4W",
                Saldo = 0,
                CustoUnitario = 0
            },
            new Insumo
            {
                TipoInsumoId = capacitor.Id,
                Nome = "Capacitor eletrolítico 10µF 35V",
                Valor = "10µF",
                Voltagem = "35V",
                Saldo = 0,
                CustoUnitario = 0
            },
            new Insumo
            {
                TipoInsumoId = capacitor.Id,
                Nome = "Capacitor cerâmico 100nF",
                Valor = "100nF",
                Saldo = 0,
                CustoUnitario = 0
            },
            new Insumo
            {
                TipoInsumoId = diodo.Id,
                Nome = "Diodo 1N4007",
                Voltagem = "1000V",
                Saldo = 0,
                CustoUnitario = 0
            },
            new Insumo
            {
                TipoInsumoId = transistor.Id,
                Nome = "Transistor BC547",
                Saldo = 0,
                CustoUnitario = 0
            },
            new Insumo
            {
                TipoInsumoId = ci.Id,
                Nome = "CI LM358",
                Saldo = 0,
                CustoUnitario = 0
            }
        );

        await db.SaveChangesAsync();
    }
}
