using Xunit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PTRON.Data;
using PTRON.Identity;
using PTRON.Models;

namespace PTRON.Tests;

public sealed class IdentityFoundationTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _root = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        await BuildAsync(new Dictionary<string, string?>());
    }

    private async Task BuildAsync(Dictionary<string, string?> settings)
    {
        if (_root is not null)
        {
            await _root.DisposeAsync();
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddPtronIdentity(configuration);
        _root = services.BuildServiceProvider();

        using var scope = _root.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _root.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static Task EnsureLeo(IServiceScope scope)
        => LeoBootstrap.EnsureAsync(scope.ServiceProvider, NullLogger.Instance);

    [Fact]
    public async Task Migration_creates_reserved_leo_without_password()
    {
        using var scope = _root.CreateScope();
        var leo = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
            .FindByIdAsync(ReservedUsers.LeoId);

        Assert.NotNull(leo);
        Assert.True(leo!.Reservado);
        Assert.True(leo.EmailConfirmed);
        Assert.Equal("pedrosa.leonardo@gmail.com", leo.Email);
        Assert.Null(leo.PasswordHash);
    }

    [Fact]
    public async Task Bootstrap_stores_initial_password_hash_and_is_idempotent()
    {
        await BuildAsync(new() { ["Leo:InitialPassword"] = "Segredo123" });

        using (var scope = _root.CreateScope())
        {
            await EnsureLeo(scope);
        }

        string? firstHash;
        using (var scope = _root.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var leo = (await users.FindByIdAsync(ReservedUsers.LeoId))!;
            firstHash = leo.PasswordHash;
            Assert.NotNull(firstHash);
            Assert.True(await users.CheckPasswordAsync(leo, "Segredo123"));
            Assert.False(await users.CheckPasswordAsync(leo, "Leoxp123"));
        }

        using (var scope = _root.CreateScope())
        {
            await EnsureLeo(scope);
            var leo = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
                .FindByIdAsync(ReservedUsers.LeoId))!;
            Assert.Equal(firstHash, leo.PasswordHash);
        }
    }

    [Fact]
    public async Task Bootstrap_does_not_overwrite_a_password_changed_later()
    {
        using var scope = _root.CreateScope();
        await EnsureLeo(scope);

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var leo = (await users.FindByIdAsync(ReservedUsers.LeoId))!;
        Assert.True(await users.CheckPasswordAsync(leo, "Leoxp123"));

        var token = await users.GeneratePasswordResetTokenAsync(leo);
        Assert.True((await users.ResetPasswordAsync(leo, token, "NovaSenha99")).Succeeded);

        await EnsureLeo(scope);
        leo = (await users.FindByIdAsync(ReservedUsers.LeoId))!;
        Assert.True(await users.CheckPasswordAsync(leo, "NovaSenha99"));
    }

    [Fact]
    public async Task Bootstrap_syncs_leo_email_from_configuration()
    {
        await BuildAsync(new() { ["Leo:Email"] = "novo@example.com" });

        using var scope = _root.CreateScope();
        await EnsureLeo(scope);
        var leo = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
            .FindByIdAsync(ReservedUsers.LeoId))!;

        Assert.Equal("novo@example.com", leo.Email);
        Assert.True(leo.EmailConfirmed);
    }

    [Theory]
    [InlineData("curta1", "senha curta")]
    [InlineData("somenteletras", "sem número")]
    [InlineData("12345678", "sem letra")]
    public async Task Password_policy_rejects_weak_passwords(string password, string _)
    {
        using var scope = _root.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var result = await users.CreateAsync(NewUser("maria@example.com"), password);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Password_policy_accepts_letter_and_number()
    {
        using var scope = _root.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var result = await users.CreateAsync(NewUser("maria@example.com"), "abcdefg1");

        Assert.True(result.Succeeded, string.Join(";", result.Errors.Select(e => e.Description)));
    }

    [Fact]
    public async Task Duplicate_email_is_rejected_case_insensitively()
    {
        using var scope = _root.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.True((await users.CreateAsync(NewUser("maria@example.com"), "abcdefg1")).Succeeded);
        Assert.False((await users.CreateAsync(NewUser("MARIA@example.com"), "abcdefg1")).Succeeded);
    }

    [Theory]
    [InlineData("pedrosa.leonardo@gmail.com")]
    [InlineData("PEDROSA.LEONARDO@gmail.com")]
    [InlineData("leo")]
    public async Task Reserved_name_and_email_cannot_be_registered(string email)
    {
        using var scope = _root.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var result = await users.CreateAsync(NewUser(email), "abcdefg1");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Identity_requires_confirmed_email_and_lockout()
    {
        using var scope = _root.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.True(options.SignIn.RequireConfirmedEmail);
        Assert.True(options.User.RequireUniqueEmail);
        Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Lockout.DefaultLockoutTimeSpan);
    }

    private static ApplicationUser NewUser(string email) => new()
    {
        UserName = email,
        Email = email,
        Pais = "BR",
        Estado = "SP"
    };
}
