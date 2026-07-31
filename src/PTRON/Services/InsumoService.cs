using Microsoft.EntityFrameworkCore;
using PTRON.Data;
using PTRON.Models;

namespace PTRON.Services;

public class InsumoService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ImageUploadService _images;

    public InsumoService(IDbContextFactory<AppDbContext> factory, ImageUploadService images)
    {
        _factory = factory;
        _images = images;
    }

    public async Task<List<Insumo>> GetAllAsync(string? search = null, int? tipoId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Insumos
            .Include(i => i.TipoInsumo)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Every token must match at least one field (nome, tipo, valor, potência, voltagem).
            foreach (var token in SplitSearchTokens(search))
            {
                var t = $"%{token}%";
                query = query.Where(i =>
                    EF.Functions.Like(i.Nome, t)
                    || EF.Functions.Like(i.TipoInsumo!.Nome, t)
                    || (i.Valor != null && EF.Functions.Like(i.Valor, t))
                    || (i.Potencia != null && EF.Functions.Like(i.Potencia, t))
                    || (i.Voltagem != null && EF.Functions.Like(i.Voltagem, t)));
            }
        }

        if (tipoId is not null)
        {
            query = query.Where(i => i.TipoInsumoId == tipoId);
        }

        return await query
            .OrderBy(i => i.Nome)
            .ThenBy(i => i.Valor)
            .ThenBy(i => i.Potencia)
            .ThenBy(i => i.Voltagem)
            .ToListAsync();
    }

    /// <summary>
    /// Matches when every whitespace-separated token appears in nome, tipo, valor, potência or voltagem.
    /// Example: "Resistor Carbono 10" matches Nome=Carbono, Tipo=Resistor, Valor=10Ω.
    /// </summary>
    public static bool MatchesSearch(Insumo i, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        foreach (var token in SplitSearchTokens(search))
        {
            if (!FieldContains(i.Nome, token)
                && !FieldContains(i.TipoInsumo?.Nome, token)
                && !FieldContains(i.Valor, token)
                && !FieldContains(i.Potencia, token)
                && !FieldContains(i.Voltagem, token))
            {
                return false;
            }
        }

        return true;
    }

    private static string[] SplitSearchTokens(string search)
        => search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool FieldContains(string? field, string token)
        => field is not null && field.Contains(token, StringComparison.OrdinalIgnoreCase);

    public async Task<Insumo?> GetAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Insumos.Include(i => i.TipoInsumo)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task CreateAsync(Insumo insumo)
    {
        await using var db = await _factory.CreateDbContextAsync();
        insumo.Nome = Validation.RequireName(insumo.Nome);
        if (insumo.TipoInsumoId == 0)
        {
            throw new InvalidOperationException("Selecione o tipo do insumo.");
        }

        insumo.Valor = string.IsNullOrWhiteSpace(insumo.Valor) ? null : insumo.Valor.Trim();
        insumo.Potencia = string.IsNullOrWhiteSpace(insumo.Potencia) ? null : insumo.Potencia.Trim();
        insumo.Voltagem = string.IsNullOrWhiteSpace(insumo.Voltagem) ? null : insumo.Voltagem.Trim();
        insumo.Saldo = 0m;
        insumo.CustoUnitario = 0m;
        db.Insumos.Add(insumo);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Insumo insumo)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.Insumos.FirstOrDefaultAsync(i => i.Id == insumo.Id)
            ?? throw new InvalidOperationException("Insumo não encontrado.");

        // Delete the previously stored photo if it was replaced or removed.
        if (!string.IsNullOrEmpty(existing.FotoPath) && existing.FotoPath != insumo.FotoPath)
        {
            _images.Delete(existing.FotoPath);
        }

        existing.Nome = Validation.RequireName(insumo.Nome);
        if (insumo.TipoInsumoId == 0)
        {
            throw new InvalidOperationException("Selecione o tipo do insumo.");
        }

        existing.TipoInsumoId = insumo.TipoInsumoId;
        existing.Valor = string.IsNullOrWhiteSpace(insumo.Valor) ? null : insumo.Valor.Trim();
        existing.Potencia = string.IsNullOrWhiteSpace(insumo.Potencia) ? null : insumo.Potencia.Trim();
        existing.Voltagem = string.IsNullOrWhiteSpace(insumo.Voltagem) ? null : insumo.Voltagem.Trim();
        existing.FotoPath = insumo.FotoPath;
        // Saldo and CustoUnitario are managed by stock entry / production, not here.

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes an insumo. Throws InvalidOperationException if it is referenced by
    /// a model (BOM), a stock entry, a product, or still has stock balance.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var insumo = await db.Insumos.FirstOrDefaultAsync(i => i.Id == id);
        if (insumo is null)
        {
            return;
        }

        if (await db.EquipamentoInsumos.AnyAsync(ei => ei.InsumoId == id))
        {
            throw new InvalidOperationException(
                "Não é possível excluir: o insumo faz parte de um ou mais equipamentos.");
        }

        if (await db.EntradaEstoqueItens.AnyAsync(it => it.InsumoId == id))
        {
            throw new InvalidOperationException(
                "Não é possível excluir: o insumo possui movimentações de estoque.");
        }

        if (await db.ProdutoInsumos.AnyAsync(pi => pi.InsumoId == id))
        {
            throw new InvalidOperationException(
                "Não é possível excluir: o insumo foi utilizado em produtos.");
        }

        if (insumo.Saldo != 0m)
        {
            throw new InvalidOperationException(
                "Não é possível excluir: o insumo possui saldo em estoque.");
        }

        _images.Delete(insumo.FotoPath);
        db.Insumos.Remove(insumo);
        await db.SaveChangesAsync();
    }
}
