using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PTRON.Data;
using PTRON.Email;
using PTRON.Identity;
using PTRON.Models;
using Xunit;

namespace PTRON.Tests;

public sealed class ActivationTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<AppDbContext> _options = null!;
    private ServiceProvider _root = null!;
    private readonly RecordingEmailSender _email = new();

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddPtronIdentity(new ConfigurationBuilder().Build());
        services.AddSingleton<IAppEmailSender>(_email);
        _root = services.BuildServiceProvider();

        using var scope = _root.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _root.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<(string UserId, string Token)> SignUpPending(string email = "maria@example.com")
    {
        using var scope = _root.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<SignUpService>().RegisterAsync(new SignUpRequest
        {
            Email = email,
            Pais = "BR",
            Estado = "SP",
            Senha = "Senha1234",
        });
        Assert.True(result.Succeeded);
        var sent = _email.Activations.Last(a => a.To == email);
        return (sent.UserId, sent.Token);
    }

    private async Task<ActivationOutcome> Activate(string? userId, string? token)
    {
        using var scope = _root.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ActivationService>().ActivateAsync(userId, token);
    }

    private async Task<List<string>> TiposOf(string userId)
    {
        await using var db = new AppDbContext(_options, userId);
        return await db.TiposInsumo.OrderBy(t => t.Id).Select(t => t.Nome).ToListAsync();
    }

    [Fact]
    public async Task Valid_link_confirms_the_email_and_creates_the_default_types()
    {
        var (userId, token) = await SignUpPending();

        Assert.Equal(ActivationOutcome.Activated, await Activate(userId, token));

        using var scope = _root.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(userId);
        Assert.True(user!.EmailConfirmed);
        Assert.Equal(new[] { "Resistor", "Capacitor", "Diodo", "Transistor", "CI", "Válvula" }, await TiposOf(userId));
    }

    [Fact]
    public async Task Default_types_are_not_created_a_second_time()
    {
        var (userId, token) = await SignUpPending();
        await Activate(userId, token);

        Assert.Equal(ActivationOutcome.AlreadyActive, await Activate(userId, token));

        Assert.Equal(6, (await TiposOf(userId)).Count);
    }

    [Fact]
    public async Task Default_types_belong_only_to_the_activated_user()
    {
        var (anaId, anaToken) = await SignUpPending("ana@example.com");
        await SignUpPending("bia@example.com");
        await Activate(anaId, anaToken);

        await using var db = new AppDbContext(_options, ReservedUsers.LeoId);
        Assert.DoesNotContain(await db.TiposInsumo.Select(t => t.UserId).ToListAsync(), id => id == anaId);
        Assert.Equal(6, (await TiposOf(anaId)).Count);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "abc")]
    [InlineData("nao-existe", "abc")]
    public async Task Missing_or_unknown_user_is_invalid(string? userId, string? token)
    {
        Assert.Equal(ActivationOutcome.Invalid, await Activate(userId, token));
    }

    [Fact]
    public async Task Wrong_token_is_invalid_and_leaves_the_account_pending()
    {
        var (userId, _) = await SignUpPending();

        Assert.Equal(ActivationOutcome.Invalid, await Activate(userId, "token-errado"));
        Assert.Equal(ActivationOutcome.Invalid, await Activate(userId, ""));

        using var scope = _root.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(userId);
        Assert.False(user!.EmailConfirmed);
        Assert.Empty(await TiposOf(userId));
    }

    [Fact]
    public async Task Token_of_another_user_is_invalid()
    {
        var (anaId, _) = await SignUpPending("ana@example.com");
        var (_, biaToken) = await SignUpPending("bia@example.com");

        Assert.Equal(ActivationOutcome.Invalid, await Activate(anaId, biaToken));
    }

    [Fact]
    public async Task Resend_sends_a_working_link_for_a_pending_account()
    {
        var (userId, _) = await SignUpPending();
        _email.Activations.Clear();

        using (var scope = _root.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ActivationService>().ResendAsync(" MARIA@example.com ");

        var sent = Assert.Single(_email.Activations);
        Assert.Equal(ActivationOutcome.Activated, await Activate(userId, sent.Token));
    }

    [Fact]
    public async Task Resend_is_silent_for_unknown_active_and_reserved_accounts()
    {
        var (userId, token) = await SignUpPending();
        await Activate(userId, token);
        _email.Activations.Clear();

        using var scope = _root.CreateScope();
        var activation = scope.ServiceProvider.GetRequiredService<ActivationService>();
        await activation.ResendAsync("maria@example.com");
        await activation.ResendAsync("ninguem@example.com");
        await activation.ResendAsync("pedrosa.leonardo@gmail.com");
        await activation.ResendAsync("not an email");
        await activation.ResendAsync(null);

        Assert.Empty(_email.Activations);
    }

    [Fact]
    public async Task Resend_shares_the_seven_per_address_limit_with_sign_up()
    {
        await SignUpPending();
        using var scope = _root.CreateScope();
        var activation = scope.ServiceProvider.GetRequiredService<ActivationService>();

        for (var i = 0; i < 10; i++)
            await activation.ResendAsync("maria@example.com");

        Assert.Equal(ActivationEmailLimiter.DefaultMaxEmails, _email.Activations.Count);
    }

    private sealed class RecordingEmailSender : IAppEmailSender
    {
        public List<(string To, string UserId, string Token)> Activations { get; } = new();

        public Task SendActivationAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default)
        {
            Activations.Add((toAddress, userId, token));
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendPasswordChangedAsync(string toAddress, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
