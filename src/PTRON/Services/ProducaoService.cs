using Microsoft.EntityFrameworkCore;
using PTRON.Data;
using PTRON.Models;

namespace PTRON.Services;

public class BomLinhaPreview
{
    public int InsumoId { get; set; }
    public string InsumoNome { get; set; } = string.Empty;
    public decimal QtdNecessaria { get; set; }
    public decimal SaldoDisponivel { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal Subtotal => QtdNecessaria * CustoUnitario;
    public decimal Faltante => Math.Max(0, QtdNecessaria - SaldoDisponivel);
    public bool Disponivel => Faltante == 0;
}

public class ProducaoPreview
{
    public int EquipamentoId { get; set; }
    public string EquipamentoNome { get; set; } = string.Empty;
    public List<BomLinhaPreview> Linhas { get; set; } = new();
    public decimal CustoEstimado => Linhas.Sum(l => l.Subtotal);
    public bool PodeProduzir => Linhas.Count > 0 && Linhas.All(l => l.Disponivel);
    public List<BomLinhaPreview> Faltantes => Linhas.Where(l => !l.Disponivel).ToList();
}

public class ProducaoService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ProducaoService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<ProducaoPreview?> GetPreviewAsync(int equipamentoId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var equipamento = await db.Equipamentos
            .Include(e => e.Insumos)
                .ThenInclude(ei => ei.Insumo)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == equipamentoId);

        if (equipamento is null)
        {
            return null;
        }

        return new ProducaoPreview
        {
            EquipamentoId = equipamento.Id,
            EquipamentoNome = equipamento.Nome,
            Linhas = equipamento.Insumos
                .OrderBy(ei => ei.Insumo!.Nome)
                .Select(ei => new BomLinhaPreview
                {
                    InsumoId = ei.InsumoId,
                    InsumoNome = ei.Insumo!.Nome,
                    QtdNecessaria = ei.Qtd,
                    SaldoDisponivel = ei.Insumo.Saldo,
                    CustoUnitario = ei.Insumo.CustoUnitario
                })
                .ToList()
        };
    }

    /// <summary>
    /// Produces one unit of the equipment model: validates stock, subtracts
    /// insumos, and creates a Produto with a cost snapshot (ProdutoInsumo).
    /// </summary>
    public async Task<Produto> ProduzirAsync(int equipamentoId, string? descricaoAdicional)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            var equipamento = await db.Equipamentos
                .Include(e => e.Insumos)
                    .ThenInclude(ei => ei.Insumo)
                .FirstOrDefaultAsync(e => e.Id == equipamentoId)
                ?? throw new InvalidOperationException("Equipamento não encontrado.");

            if (equipamento.Insumos.Count == 0)
            {
                throw new InvalidOperationException(
                    "O equipamento não possui insumos na lista (BOM).");
            }

            var faltantes = new List<string>();
            foreach (var bom in equipamento.Insumos)
            {
                var insumo = bom.Insumo!;
                if (insumo.Saldo < bom.Qtd)
                {
                    faltantes.Add(
                        $"{insumo.Nome}: necessário {bom.Qtd:0.####}, disponível {insumo.Saldo:0.####}");
                }
            }

            if (faltantes.Count > 0)
            {
                throw new InvalidOperationException(
                    "Não é possível produzir — insumos insuficientes:\n" + string.Join("\n", faltantes));
            }

            var snapshots = new List<ProdutoInsumo>();
            decimal custoTotal = 0m;

            foreach (var bom in equipamento.Insumos)
            {
                var insumo = bom.Insumo!;
                // Snapshot the current average cost at production time.
                var preco = insumo.CustoUnitario;
                snapshots.Add(new ProdutoInsumo
                {
                    InsumoId = insumo.Id,
                    Qtd = bom.Qtd,
                    PrecoUnitario = preco
                });
                custoTotal += bom.Qtd * preco;
                insumo.Saldo -= bom.Qtd;
            }

            var produto = new Produto
            {
                EquipamentoId = equipamento.Id,
                DescricaoAdicional = string.IsNullOrWhiteSpace(descricaoAdicional)
                    ? null
                    : descricaoAdicional.Trim(),
                Data = DateTime.Now,
                CustoTotal = custoTotal,
                Insumos = snapshots
            };

            db.Produtos.Add(produto);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return produto;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
