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
                .ThenInclude(i => i.Insumo!)
                    .ThenInclude(ins => ins.TipoInsumo)
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
                .ThenInclude(i => i.Insumo!)
                    .ThenInclude(ins => ins.TipoInsumo)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public EntradaRateio Preview(EntradaCommand command) => EntradaCustoCalculator.Calcular(command);

    /// <summary>
    /// Finalizes a stock entry: persists the cart, allocates shipment and taxes
    /// by each product's share of the product total, converts each currency to BRL
    /// with its own rate, and recalculates the weighted-average unit cost inside a transaction.
    /// Formula: novoCusto = (Saldo*CustoUnitario + Σ qtd*custoAterrado) / (Saldo + Σ qtd)
    /// </summary>
    public async Task<EntradaEstoque> FinalizarAsync(EntradaCommand command)
    {
        var rateio = EntradaCustoCalculator.Calcular(command);

        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            var entrada = new EntradaEstoque
            {
                Data = DateTime.Now,
                MoedaProdutos = rateio.MoedaProdutos,
                CambioProdutos = rateio.CambioProdutos,
                MoedaImpostos = rateio.MoedaImpostos,
                CambioImpostos = rateio.CambioImpostos,
                MoedaFrete = rateio.MoedaFrete,
                MoedaDestino = rateio.MoedaDestino,
                Frete = rateio.Frete,
                Impostos = rateio.Impostos,
                Itens = rateio.Linhas.Select(a => new EntradaEstoqueItem
                {
                    InsumoId = a.InsumoId,
                    Qtd = a.Qtd,
                    PrecoUnitario = a.PrecoUnitario,
                    FreteRateado = a.FreteRateado,
                    ImpostoRateado = a.ImpostoRateado,
                    CustoUnitario = a.CustoUnitarioBrl
                }).ToList()
            };

            db.EntradasEstoque.Add(entrada);

            foreach (var item in rateio.Linhas)
            {
                var insumo = await db.Insumos.FirstOrDefaultAsync(i => i.Id == item.InsumoId)
                    ?? throw new InvalidOperationException($"Insumo {item.InsumoId} não encontrado.");

                var saldoAnterior = insumo.Saldo;
                var custoAnterior = insumo.CustoUnitario;
                var novoSaldo = saldoAnterior + item.Qtd;

                insumo.CustoUnitario = novoSaldo == 0
                    ? item.CustoUnitarioBrl
                    : (saldoAnterior * custoAnterior + item.Qtd * item.CustoUnitarioBrl) / novoSaldo;

                insumo.Saldo = novoSaldo;
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return entrada;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
