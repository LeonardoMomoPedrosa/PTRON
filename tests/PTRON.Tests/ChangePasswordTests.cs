using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PTRON.Data;
using PTRON.Email;
using PTRON.Identity;
using PTRON.Models;
using Xunit;

namespace PTRON.Tests;

public sealed class ChangePasswordTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _root = null!;
    private readonly RecordingEmailSender _email = new();
    private string _anaId = null!;
    private string _pendingId = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Leo:InitialPassword"] = "Segredo123" })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddPtronIdentity(configuration);
        services.AddSingleton<IAppEmailSender>(_email);
        _root = services.BuildServiceProvider();

        using var scope = _root.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        await LeoBootstrap.EnsureAsync(scope.ServiceProvider, NullLogger.Instance);

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var ana = new ApplicationUser { UserName = "ana@example.com", Email = "ana@example.com", EmailConfirmed = true };
        Assert.True((await users.CreateAsync(ana, "Senha1234")).Succeeded);
        _anaId = ana.Id;

        var pending = new ApplicationUser { UserName = "pendente@example.com", Email = "pendente@example.com" };
        Assert.True((await users.CreateAsync(pending, "Senha1234")).Succeeded);
        _pendingId = pending.Id;
    }

    public async Task DisposeAsync()
    {
        await _root.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<ChangePasswordResult> Change(string? userId, string? current, string? next, string? confirm)
    {
        using var scope = _root.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ChangePasswordService>()
            .ChangeAsync(userId, current, next, confirm);
    }

    private async Task<bool> CheckPassword(string email, string password)
    {
        using var scope = _root.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.CheckPasswordAsync((await users.FindByEmailAsync(email))!, password);
    }

    private async Task<string?> Stamp(string userId)
    {
        using var scope = _root.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return (await users.FindByIdAsync(userId))?.SecurityStamp;
    }

    private SessionStampValidator Validator()
        => new( _root.GetRequiredService<DbContextOptions<AppDbContext>>(), NullLogger<SessionStampValidator>.Instance);

    [Fact]
    public async Task Change_rotates_the_stamp_sends_notice_and_rejects_the_old_session()
    {
        var before = await Stamp(_anaId);

        var result = await Change(_anaId, "Senha1234", "NovaSenha99", "NovaSenha99");

        Assert.True(result.Succeeded);
        Assert.True(await CheckPassword("ana@example.com", "NovaSenha99"));
        Assert.False(await CheckPassword("ana@example.com", "Senha1234"));
        Assert.Equal("ana@example.com", Assert.Single(_email.PasswordChanged));

        var after = await Stamp(_anaId);
        Assert.NotEqual(before, after);
        var validator = Validator();
        Assert.False(await validator.IsValidAsync(_anaId, before));
        Assert.True(await validator.IsValidAsync(_anaId, after));
    }

    [Fact]
    public async Task Wrong_current_password_changes_nothing()
    {
        var before = await Stamp(_anaId);

        var result = await Change(_anaId, "errada", "NovaSenha99", "NovaSenha99");

        Assert.False(result.Succeeded);
        Assert.Contains("Senha incorreta.", result.Errors);
        Assert.Empty(_email.PasswordChanged);
        Assert.Equal(before, await Stamp(_anaId));
        Assert.True(await CheckPassword("ana@example.com", "Senha1234"));
        Assert.True(await Validator().IsValidAsync(_anaId, before));
    }

    [Fact]
    public async Task Confirmation_must_match()
    {
        var result = await Change(_anaId, "Senha1234", "NovaSenha99", "OutraSenha99");

        Assert.False(result.Succeeded);
        Assert.Contains("As senhas não conferem.", result.Errors);
        Assert.True(await CheckPassword("ana@example.com", "Senha1234"));
        Assert.Empty(_email.PasswordChanged);
    }

    [Fact]
    public async Task Weak_password_is_rejected()
    {
        var result = await Change(_anaId, "Senha1234", "curta", "curta");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
        Assert.True(await CheckPassword("ana@example.com", "Senha1234"));
        Assert.Empty(_email.PasswordChanged);
    }

    [Fact]
    public async Task Inactive_account_cannot_change_password()
    {
        var result = await Change(_pendingId, "Senha1234", "NovaSenha99", "NovaSenha99");

        Assert.False(result.Succeeded);
        Assert.Contains("A conta ainda não está ativa.", result.Errors);
        Assert.True(await CheckPassword("pendente@example.com", "Senha1234"));
        Assert.False(await Validator().IsValidAsync(_pendingId, await Stamp(_pendingId)));
    }

    [Fact]
    public async Task Leo_can_change_password_and_keeps_the_reserved_claim()
    {
        var result = await Change(ReservedUsers.LeoId, "Segredo123", "OutraSenha77", "OutraSenha77");

        Assert.True(result.Succeeded);
        Assert.True(await CheckPassword("pedrosa.leonardo@gmail.com", "OutraSenha77"));

        using var scope = _root.CreateScope();
        var leo = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
            .FindByIdAsync(ReservedUsers.LeoId))!;
        var principal = AuthPrincipal.Create(leo);
        Assert.True(ReservedUsers.IsReservedPrincipal(principal));
        Assert.Equal(leo.SecurityStamp, principal.FindFirstValue(SessionStampValidator.StampClaimType));
        Assert.True(await Validator().IsValidAsync(ReservedUsers.LeoId, leo.SecurityStamp));
    }

    [Fact]
    public void Old_leo_cookie_without_the_reserved_claim_is_still_recognized()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, ReservedUsers.LeoId) },
            "Cookies");

        Assert.True(ReservedUsers.IsReservedPrincipal(new ClaimsPrincipal(identity)));
    }

    [Fact]
    public void Regular_user_is_not_reserved()
    {
        var ana = new ApplicationUser
        {
            Id = _anaId,
            UserName = "ana@example.com",
            Email = "ana@example.com",
            SecurityStamp = "stamp"
        };

        Assert.False(ReservedUsers.IsReservedPrincipal(AuthPrincipal.Create(ana)));
    }

    private sealed class RecordingEmailSender : IAppEmailSender
    {
        public List<string> PasswordChanged { get; } = new();

        public Task SendActivationAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendPasswordResetAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendPasswordChangedAsync(string toAddress, CancellationToken cancellationToken = default)
        {
            PasswordChanged.Add(toAddress);
            return Task.CompletedTask;
        }
    }
}
