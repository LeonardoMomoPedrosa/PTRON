using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PTRON.Data;
using PTRON.Services;
using Xunit;

namespace PTRON.Tests;

public sealed class EntradaTresMoedasMigrationTests
{
    [Fact]
    public async Task Entrada_de_uma_moeda_vira_tres_moedas_sem_mudar_o_custo()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var db = new AppDbContext(options, "leo"))
        {
            var migrator = db.Database.GetService<IMigrator>()
                ?? throw new InvalidOperationException("Migrator indisponível.");
            await migrator.MigrateAsync("20261002010339_UserOwnership");

            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO TiposInsumo (Id, Nome, UserId) VALUES (1, 'Resistor', 'leo');
                INSERT INTO Insumos (Id, TipoInsumoId, Nome, Saldo, CustoUnitario, UserId)
                VALUES (1, 1, '10k', '0', '0', 'leo');
                INSERT INTO EntradasEstoque (Id, Data, Frete, Impostos, Moeda, Cambio, UserId)
                VALUES (1, '2026-01-01 00:00:00', '10.0', '5.0', 'USD', '5.5', 'leo');
                INSERT INTO EntradaEstoqueItens
                    (Id, EntradaEstoqueId, InsumoId, Qtd, PrecoUnitario, FreteRateado, ImpostoRateado, CustoUnitario)
                VALUES (1, 1, 1, '2.0', '10.0', '4.0', '2.0', '71.5');
                """);

            await db.Database.MigrateAsync();
        }

        await using var atual = new AppDbContext(options, "leo");
        var entrada = await atual.EntradasEstoque.Include(e => e.Itens).SingleAsync();

        Assert.Equal(Moedas.Usd, entrada.MoedaProdutos);
        Assert.Equal(5.5m, entrada.CambioProdutos);
        Assert.Equal(Moedas.Usd, entrada.MoedaImpostos);
        Assert.Equal(5.5m, entrada.CambioImpostos);
        Assert.Equal(Moedas.Usd, entrada.MoedaFrete);
        Assert.Equal(Moedas.Destino, entrada.MoedaDestino);
        Assert.Equal(71.5m, entrada.Itens.Single().CustoUnitario);

        // Old formula: (products + freight + taxes) × the single rate.
        var totalAntigo = (entrada.TotalProdutos + entrada.Frete + entrada.Impostos) * 5.5m;
        Assert.Equal(totalAntigo, entrada.TotalBrl);
    }
}
