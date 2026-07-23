using Microsoft.EntityFrameworkCore;
using PTRON.Data;
using PTRON.Models;

namespace PTRON.Services;

public class EntradaEstoqueService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public EntradaEstoqueService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<EntradaEstoque>> GetHistoricoAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.EntradasEstoque
            .Include(e => e.Itens)
                .ThenInclude(i => i.Insumo)
            .AsNoTracking()
            .OrderByDescending(e => e.Data)
            .ThenByDescending(e => e.Id)
            .ToListAsync();
    }

    public async Task<EntradaEstoque?> GetAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.EntradasEstoque
            .Include(e => e.Itens)
                .ThenInclude(i => i.Insumo)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// Finalizes a stock entry: persists the cart and recalculates the
    /// weighted-average unit cost for each affected insumo inside a transaction.
    /// Formula: novoCusto = (Saldo*CustoUnitario + Σ qtd*preço) / (Saldo + Σ qtd)
    /// </summary>
    public async Task FinalizarAsync(IReadOnlyList<EntradaEstoqueItem> itens)
    {
        if (itens is null || itens.Count == 0)
        {
            throw new InvalidOperationException("Adicione ao menos um item à entrada.");
        }

        if (itens.Any(i => i.InsumoId == 0 || i.Qtd <= 0 || i.PrecoUnitario < 0))
        {
            throw new InvalidOperationException(
                "Todos os itens devem ter insumo, quantidade maior que zero e preço não negativo.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            // Aggregate duplicate insumos in the cart before applying.
            var agregados = itens
                .GroupBy(i => i.InsumoId)
                .Select(g => new
                {
                    InsumoId = g.Key,
                    Qtd = g.Sum(x => x.Qtd),
                    // Weighted average of the prices in this cart for the same insumo.
                    PrecoUnitario = g.Sum(x => x.Qtd * x.PrecoUnitario) / g.Sum(x => x.Qtd)
                })
                .ToList();

            var entrada = new EntradaEstoque
            {
                Data = DateTime.Now,
                Itens = agregados.Select(a => new EntradaEstoqueItem
                {
                    InsumoId = a.InsumoId,
                    Qtd = a.Qtd,
                    PrecoUnitario = a.PrecoUnitario
                }).ToList()
            };

            db.EntradasEstoque.Add(entrada);

            foreach (var item in agregados)
            {
                var insumo = await db.Insumos.FirstOrDefaultAsync(i => i.Id == item.InsumoId)
                    ?? throw new InvalidOperationException($"Insumo {item.InsumoId} não encontrado.");

                var saldoAnterior = insumo.Saldo;
                var custoAnterior = insumo.CustoUnitario;
                var novoSaldo = saldoAnterior + item.Qtd;

                // When previous saldo is zero, the new unit cost is simply the entry price.
                insumo.CustoUnitario = novoSaldo == 0
                    ? item.PrecoUnitario
                    : (saldoAnterior * custoAnterior + item.Qtd * item.PrecoUnitario) / novoSaldo;

                insumo.Saldo = novoSaldo;
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
