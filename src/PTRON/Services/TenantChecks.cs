using Microsoft.EntityFrameworkCore;
using PTRON.Data;

namespace PTRON.Services;

/// <summary>
/// Confirms a referenced root row belongs to the current user. Query filters hide
/// other users' rows, so a missing id is reported as not found.
/// </summary>
internal static class TenantChecks
{
    public static async Task RequireTipoAsync(AppDbContext db, int tipoId)
    {
        if (!await db.TiposInsumo.AnyAsync(t => t.Id == tipoId))
        {
            throw new InvalidOperationException("Tipo não encontrado.");
        }
    }

    public static async Task RequireInsumosAsync(AppDbContext db, IEnumerable<int> insumoIds)
    {
        var ids = insumoIds.Where(id => id != 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var found = await db.Insumos.CountAsync(i => ids.Contains(i.Id));
        if (found != ids.Count)
        {
            throw new InvalidOperationException("Insumo não encontrado.");
        }
    }
}
