using Microsoft.EntityFrameworkCore;
using PTRON.Data;
using PTRON.Models;

namespace PTRON.Services;

public class ProdutoService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ProdutoService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<Produto>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Produtos
            .Include(p => p.Equipamento)
            .AsNoTracking()
            .OrderByDescending(p => p.Data)
            .ThenByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<Produto?> GetWithInsumosAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Produtos
            .Include(p => p.Equipamento)
            .Include(p => p.Insumos)
                .ThenInclude(pi => pi.Insumo)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Deletes a produced product and returns its consumed insumos to stock (quantity only).
    /// The product cost snapshot is discarded with the product; existing averages are left unchanged.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            var produto = await db.Produtos
                .Include(p => p.Insumos)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new InvalidOperationException("Produto não encontrado.");

            foreach (var item in produto.Insumos)
            {
                var insumo = await db.Insumos.FirstOrDefaultAsync(i => i.Id == item.InsumoId);
                if (insumo is not null)
                {
                    insumo.Saldo += item.Qtd;
                }
            }

            db.Produtos.Remove(produto); // cascade removes ProdutoInsumo snapshot rows
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
