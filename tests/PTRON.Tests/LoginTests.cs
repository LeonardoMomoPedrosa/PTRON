using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PTRON.Data;
using PTRON.Identity;
using PTRON.Models;
using Xunit;

namespace PTRON.Tests;

public sealed class LoginTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _root = null!;

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
        _root = services.BuildServiceProvider();

        using var scope = _root.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        await LeoBootstrap.EnsureAsync(scope.ServiceProvider, NullLogger.Instance);

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await Create(users, "ativa@example.com", confirmed: true);
        await Create(users, "pendente@example.com", confirmed: false);
    }

    private static async Task Create(UserManager<ApplicationUser> users, string email, bool confirmed)
    {
        var result = await users.CreateAsync(
            new ApplicationUser { UserName = email, Email = email, EmailConfirmed = confirmed, Pais = "BR", Estado = "SP" },
            "Senha1234");
        Assert.True(result.Succeeded);
    }

    public async Task DisposeAsync()
    {
        await _root.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<LoginResult> Login(string? identifier, string? password)
    {
        using var scope = _root.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<LoginService>().LoginAsync(identifier, password);
    }

    [Theory]
    [InlineData("ativa@example.com")]
    [InlineData("  ATIVA@Example.com ")]
    public async Task Active_user_signs_in_with_email_case_insensitively(string email)
    {
        var result = await Login(email, "Senha1234");

        Assert.Equal(LoginOutcome.Succeeded, result.Outcome);
        Assert.Equal("ativa@example.com", result.User!.Email);
    }

    [Theory]
    [InlineData("Leo")]
    [InlineData("leo")]
    [InlineData("pedrosa.leonardo@gmail.com")]
    public async Task Leo_signs_in_with_alias_or_email(string identifier)
    {
        var result = await Login(identifier, "Segredo123");

        Assert.Equal(LoginOutcome.Succeeded, result.Outcome);
        Assert.Equal(ReservedUsers.LeoId, result.User!.Id);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_user_get_the_same_generic_outcome()
    {
        var wrong = await Login("ativa@example.com", "SenhaErrada1");
        var unknown = await Login("ninguem@example.com", "Senha1234");
        var empty = await Login("", "");

        Assert.Equal(LoginOutcome.InvalidCredentials, wrong.Outcome);
        Assert.Equal(LoginOutcome.InvalidCredentials, unknown.Outcome);
        Assert.Equal(LoginOutcome.InvalidCredentials, empty.Outcome);
        Assert.Null(wrong.User);
    }

    [Fact]
    public async Task Pending_user_cannot_sign_in_and_is_told_only_with_the_right_password()
    {
        var right = await Login("pendente@example.com", "Senha1234");
        var wrong = await Login("pendente@example.com", "SenhaErrada1");

        Assert.Equal(LoginOutcome.NotActivated, right.Outcome);
        Assert.Equal("pendente@example.com", right.User!.Email);
        Assert.Equal(LoginOutcome.InvalidCredentials, wrong.Outcome);
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_even_for_the_right_password()
    {
        for (var i = 0; i < 4; i++)
            Assert.Equal(LoginOutcome.InvalidCredentials, (await Login("ativa@example.com", "SenhaErrada1")).Outcome);

        Assert.Equal(LoginOutcome.LockedOut, (await Login("ativa@example.com", "SenhaErrada1")).Outcome);
        Assert.Equal(LoginOutcome.LockedOut, (await Login("ativa@example.com", "Senha1234")).Outcome);
    }

    [Fact]
    public async Task Successful_login_resets_the_failed_attempt_counter()
    {
        for (var i = 0; i < 3; i++)
            await Login("ativa@example.com", "SenhaErrada1");
        Assert.Equal(LoginOutcome.Succeeded, (await Login("ativa@example.com", "Senha1234")).Outcome);

        for (var i = 0; i < 3; i++)
            await Login("ativa@example.com", "SenhaErrada1");

        Assert.Equal(LoginOutcome.Succeeded, (await Login("ativa@example.com", "Senha1234")).Outcome);
    }
}
