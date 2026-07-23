using Microsoft.EntityFrameworkCore;
using PTRON.Data;
using PTRON.Models;

namespace PTRON.Services;

public class EquipamentoService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ImageUploadService _images;

    public EquipamentoService(IDbContextFactory<AppDbContext> factory, ImageUploadService images)
    {
        _factory = factory;
        _images = images;
    }

    public async Task<List<Equipamento>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Equipamentos
            .Include(e => e.Insumos)
            .AsNoTracking()
            .OrderBy(e => e.Nome)
            .ToListAsync();
    }

    /// <summary>Loads a model with its BOM (including insumo details).</summary>
    public async Task<Equipamento?> GetWithBomAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Equipamentos
            .Include(e => e.Insumos)
                .ThenInclude(ei => ei.Insumo)
                    .ThenInclude(i => i!.TipoInsumo)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task CreateAsync(Equipamento equipamento)
    {
        await using var db = await _factory.CreateDbContextAsync();
        equipamento.Nome = Validation.RequireName(equipamento.Nome);
        NormalizeBom(equipamento);
        db.Equipamentos.Add(equipamento);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Equipamento equipamento)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.Equipamentos
            .Include(e => e.Insumos)
            .FirstOrDefaultAsync(e => e.Id == equipamento.Id)
            ?? throw new InvalidOperationException("Equipamento não encontrado.");

        if (!string.IsNullOrEmpty(existing.FotoPath) && existing.FotoPath != equipamento.FotoPath)
        {
            _images.Delete(existing.FotoPath);
        }

        existing.Nome = Validation.RequireName(equipamento.Nome);
        existing.FotoPath = equipamento.FotoPath;

        // Replace the BOM wholesale. Existing products keep their own snapshots,
        // so changing a model's BOM never affects products already produced.
        db.EquipamentoInsumos.RemoveRange(existing.Insumos);
        NormalizeBom(equipamento);
        foreach (var item in equipamento.Insumos)
        {
            existing.Insumos.Add(new EquipamentoInsumo
            {
                InsumoId = item.InsumoId,
                Qtd = item.Qtd
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a model and its BOM. Blocked when products reference the model,
    /// so already-produced products are preserved.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var equipamento = await db.Equipamentos.FirstOrDefaultAsync(e => e.Id == id);
        if (equipamento is null)
        {
            return;
        }

        if (await db.Produtos.AnyAsync(p => p.EquipamentoId == id))
        {
            throw new InvalidOperationException(
                "Não é possível excluir: existem produtos produzidos com este equipamento.");
        }

        _images.Delete(equipamento.FotoPath);
        db.Equipamentos.Remove(equipamento); // BOM is removed via cascade delete.
        await db.SaveChangesAsync();
    }

    /// <summary>Merges duplicate insumos and drops invalid/zero rows.</summary>
    private static void NormalizeBom(Equipamento equipamento)
    {
        var merged = equipamento.Insumos
            .Where(ei => ei.InsumoId != 0 && ei.Qtd > 0)
            .GroupBy(ei => ei.InsumoId)
            .Select(g => new EquipamentoInsumo
            {
                InsumoId = g.Key,
                Qtd = g.Sum(x => x.Qtd)
            })
            .ToList();

        equipamento.Insumos = merged;
    }
}
