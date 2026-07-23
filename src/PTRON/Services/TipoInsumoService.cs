using Microsoft.EntityFrameworkCore;
using PTRON.Data;
using PTRON.Models;

namespace PTRON.Services;

public class TipoInsumoService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TipoInsumoService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<TipoInsumo>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.TiposInsumo
            .OrderBy(t => t.Nome)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<TipoInsumo?> GetAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.TiposInsumo.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<int> CountInsumosAsync(int tipoId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Insumos.CountAsync(i => i.TipoInsumoId == tipoId);
    }

    public async Task CreateAsync(TipoInsumo tipo)
    {
        await using var db = await _factory.CreateDbContextAsync();
        tipo.Nome = Validation.RequireName(tipo.Nome);

        var exists = await db.TiposInsumo.AnyAsync(t => t.Nome.ToLower() == tipo.Nome.ToLower());
        if (exists)
        {
            throw new InvalidOperationException($"Já existe um tipo com o nome \"{tipo.Nome}\".");
        }

        db.TiposInsumo.Add(tipo);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(TipoInsumo tipo)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.TiposInsumo.FirstOrDefaultAsync(t => t.Id == tipo.Id)
            ?? throw new InvalidOperationException("Tipo não encontrado.");

        var nome = Validation.RequireName(tipo.Nome);
        var duplicate = await db.TiposInsumo.AnyAsync(t =>
            t.Id != tipo.Id && t.Nome.ToLower() == nome.ToLower());
        if (duplicate)
        {
            throw new InvalidOperationException($"Já existe um tipo com o nome \"{nome}\".");
        }

        existing.Nome = nome;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a type. Throws InvalidOperationException if there are linked insumos.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var hasInsumos = await db.Insumos.AnyAsync(i => i.TipoInsumoId == id);
        if (hasInsumos)
        {
            throw new InvalidOperationException(
                "Não é possível excluir: existem insumos vinculados a este tipo.");
        }

        var tipo = await db.TiposInsumo.FirstOrDefaultAsync(t => t.Id == id);
        if (tipo is not null)
        {
            db.TiposInsumo.Remove(tipo);
            await db.SaveChangesAsync();
        }
    }
}
