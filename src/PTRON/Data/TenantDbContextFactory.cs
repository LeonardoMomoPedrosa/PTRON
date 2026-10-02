using Microsoft.EntityFrameworkCore;
using PTRON.Identity;

namespace PTRON.Data;

/// <summary>
/// Creates an <see cref="AppDbContext"/> bound to <see cref="ICurrentUser"/>.
/// Blazor Server builds contexts outside the HTTP request, so the user id has to
/// travel with the factory rather than with a scoped context captured at startup.
/// </summary>
public sealed class TenantDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly ICurrentUser _currentUser;

    public TenantDbContextFactory(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    {
        _options = options;
        _currentUser = currentUser;
    }

    public AppDbContext CreateDbContext()
        => new(_options, _currentUser.UserId);
}
