using Microsoft.EntityFrameworkCore;
using PTRON.Data;
using PTRON.Identity;

namespace PTRON.Services;

public sealed record SignupCounts(int Cadastrados, int Ativos);

/// <summary>Signup totals for the reserved account. Leo is not counted: that account is not a signup.</summary>
public class SignupDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ICurrentUser _currentUser;

    public SignupDashboardService(IDbContextFactory<AppDbContext> factory, ICurrentUser currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    public async Task<SignupCounts> GetAsync()
    {
        var userId = _currentUser.UserId;
        await using var db = await _factory.CreateDbContextAsync();
        if (string.IsNullOrEmpty(userId)
            || !await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.Reservado))
        {
            throw new InvalidOperationException("O painel de cadastros é exclusivo do usuário reservado.");
        }

        var rows = await db.Users.AsNoTracking()
            .Where(u => !u.Reservado)
            .GroupBy(u => u.EmailConfirmed)
            .Select(g => new { Ativo = g.Key, Total = g.Count() })
            .ToListAsync();

        var ativos = rows.Where(r => r.Ativo).Sum(r => r.Total);
        var cadastrados = rows.Sum(r => r.Total);
        return new SignupCounts(cadastrados, ativos);
    }
}
