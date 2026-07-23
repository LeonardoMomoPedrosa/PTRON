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
            var term = search.Trim();
            query = query.Where(i => EF.Functions.Like(i.Nome, $"%{term}%"));
        }

        if (tipoId is not null)
        {
            query = query.Where(i => i.TipoInsumoId == tipoId);
        }

        return await query.OrderBy(i => i.Nome).ToListAsync();
    }

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
