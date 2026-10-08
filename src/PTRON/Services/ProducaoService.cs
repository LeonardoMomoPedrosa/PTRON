using Microsoft.EntityFrameworkCore;
using PTRON.Data;
using PTRON.Models;

namespace PTRON.Services;

public class BomLinhaPreview
{
    public int InsumoId { get; set; }
    public string InsumoNome { get; set; } = string.Empty;
    public string InsumoDetalhe { get; set; } = string.Empty;

    /// <summary>Quantity of this component on the model, for a single product.</summary>
    public decimal QtdPorUnidade { get; set; }

    /// <summary>Quantity consumed by the whole batch (per unit × how many products).</summary>
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
    public int Quantidade { get; set; } = 1;
    public List<BomLinhaPreview> Linhas { get; set; } = new();

    /// <summary>Landed cost of the whole batch.</summary>
    public decimal CustoEstimado => Linhas.Sum(l => l.Subtotal);

    /// <summary>Landed cost of one product.</summary>
    public decimal CustoPorUnidade => Quantidade == 0 ? 0m : CustoEstimado / Quantidade;

    public bool PodeProduzir => Linhas.Count > 0 && Linhas.All(l => l.Disponivel);
    public List<BomLinhaPreview> Faltantes => Linhas.Where(l => !l.Disponivel).ToList();
}

public sealed class ProducaoResultado
{
    public int Quantidade { get; init; }
    public decimal CustoUnitario { get; init; }
    public IReadOnlyList<Produto> Produtos { get; init; } = Array.Empty<Produto>();
}

public class ProducaoService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ProducaoService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<ProducaoPreview?> GetPreviewAsync(int equipamentoId, int quantidade = 1)
    {
        if (quantidade < 1)
        {
            throw new InvalidOperationException("Informe quantos produtos produzir (pelo menos 1).");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var equipamento = await db.Equipamentos
            .Include(e => e.Insumos)
                .ThenInclude(ei => ei.Insumo)
                    .ThenInclude(i => i!.TipoInsumo)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == equipamentoId);

        if (equipamento is null)
        {
            return null;
        }

        if (equipamento.Insumos.Any(ei => ei.Insumo is null))
        {
            return null;
        }

        return new ProducaoPreview
        {
            EquipamentoId = equipamento.Id,
            EquipamentoNome = equipamento.Nome,
            Quantidade = quantidade,
            Linhas = equipamento.Insumos
                .OrderBy(ei => ei.Insumo!.Nome)
                .Select(ei => new BomLinhaPreview
                {
                    InsumoId = ei.InsumoId,
                    InsumoNome = ei.Insumo!.Nome,
                    InsumoDetalhe = Format.InsumoDetalhe(ei.Insumo),
                    QtdPorUnidade = ei.Qtd,
                    QtdNecessaria = ei.Qtd * quantidade,
                    SaldoDisponivel = ei.Insumo.Saldo,
                    CustoUnitario = ei.Insumo.CustoUnitario
                })
                .ToList()
        };
    }

    /// <summary>
    /// Produces <paramref name="quantidade"/> units of the equipment model.
    /// Each unit becomes its own product, with a snapshot of one unit's components.
    /// Stock is reduced by the bill of materials times the quantity.
    /// </summary>
    public async Task<ProducaoResultado> ProduzirAsync(
        int equipamentoId, string? descricaoAdicional, int quantidade = 1)
    {
        if (quantidade < 1)
        {
            throw new InvalidOperationException("Informe quantos produtos produzir (pelo menos 1).");
        }

        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            var equipamento = await db.Equipamentos
                .Include(e => e.Insumos)
                    .ThenInclude(ei => ei.Insumo)
                        .ThenInclude(i => i!.TipoInsumo)
                .FirstOrDefaultAsync(e => e.Id == equipamentoId)
                ?? throw new InvalidOperationException("Equipamento não encontrado.");

            if (equipamento.Insumos.Any(ei => ei.Insumo is null))
            {
                throw new InvalidOperationException("Insumo não encontrado.");
            }

            if (equipamento.Insumos.Count == 0)
            {
                throw new InvalidOperationException(
                    "O equipamento não possui insumos na lista (BOM).");
            }

            var faltantes = new List<string>();
            foreach (var bom in equipamento.Insumos)
            {
                var insumo = bom.Insumo!;
                var necessario = bom.Qtd * quantidade;
                if (insumo.Saldo < necessario)
                {
                    var detalhe = Format.InsumoDetalhe(insumo);
                    var sufixo = detalhe.Length > 0 ? " (" + detalhe + ")" : string.Empty;
                    faltantes.Add(
                        $"{insumo.Nome}{sufixo}: necessário {necessario:0.####}, disponível {insumo.Saldo:0.####}");
                }
            }

            if (faltantes.Count > 0)
            {
                var unidades = quantidade == 1 ? "produzir" : $"produzir {quantidade} unidades";
                throw new InvalidOperationException(
                    $"Não é possível {unidades} — insumos insuficientes:\n" + string.Join("\n", faltantes));
            }

            var descricao = string.IsNullOrWhiteSpace(descricaoAdicional)
                ? null
                : descricaoAdicional.Trim();
            var agora = DateTime.Now;
            decimal custoUnitario = 0m;
            var produtos = new List<Produto>(quantidade);

            for (var n = 0; n < quantidade; n++)
            {
                var snapshots = new List<ProdutoInsumo>();
                decimal custo = 0m;
                foreach (var bom in equipamento.Insumos)
                {
                    var insumo = bom.Insumo!;
                    var preco = insumo.CustoUnitario;
                    snapshots.Add(new ProdutoInsumo
                    {
                        InsumoId = insumo.Id,
                        Qtd = bom.Qtd,
                        PrecoUnitario = preco
                    });
                    custo += bom.Qtd * preco;
                }

                custoUnitario = custo;
                produtos.Add(new Produto
                {
                    EquipamentoId = equipamento.Id,
                    DescricaoAdicional = descricao,
                    Data = agora,
                    CustoTotal = custo,
                    Insumos = snapshots
                });
            }

            foreach (var bom in equipamento.Insumos)
            {
                bom.Insumo!.Saldo -= bom.Qtd * quantidade;
            }

            db.Produtos.AddRange(produtos);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return new ProducaoResultado
            {
                Quantidade = quantidade,
                CustoUnitario = custoUnitario,
                Produtos = produtos
            };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
