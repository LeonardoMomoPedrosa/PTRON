using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PTRON.Data;
using PTRON.Email;
using PTRON.Identity;
using PTRON.Models;
using Xunit;

namespace PTRON.Tests;

public sealed class PasswordResetTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _root = null!;
    private readonly RecordingEmailSender _email = new();
    private string _anaId = null!;

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
        Assert.True((await users.CreateAsync(
            new ApplicationUser { UserName = "pendente@example.com", Email = "pendente@example.com" }, "Senha1234")).Succeeded);
    }

    public async Task DisposeAsync()
    {
        await _root.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task Request(string? email, string? ip = "203.0.113.5")
    {
        using var scope = _root.CreateScope();
        await scope.ServiceProvider.GetRequiredService<PasswordResetService>().RequestAsync(email, ip);
    }

    private async Task<PasswordResetResult> Reset(string? userId, string? token, string? password)
    {
        using var scope = _root.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PasswordResetService>().ResetAsync(userId, token, password);
    }

    private async Task<bool> IsValid(string? userId, string? token)
    {
        using var scope = _root.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PasswordResetService>().IsLinkValidAsync(userId, token);
    }

    private async Task<bool> CheckPassword(string email, string password)
    {
        using var scope = _root.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.CheckPasswordAsync((await users.FindByEmailAsync(email))!, password);
    }

    [Fact]
    public async Task Active_account_gets_a_link_that_resets_the_password()
    {
        await Request("ANA@example.com");

        var sent = Assert.Single(_email.Resets);
        Assert.Equal("ana@example.com", sent.To);
        Assert.Equal(_anaId, sent.UserId);
        Assert.True(await IsValid(sent.UserId, sent.Token));

        var result = await Reset(sent.UserId, sent.Token, "NovaSenha99");

        Assert.True(result.Succeeded);
        Assert.True(await CheckPassword("ana@example.com", "NovaSenha99"));
        Assert.False(await CheckPassword("ana@example.com", "Senha1234"));
        Assert.Equal("ana@example.com", Assert.Single(_email.PasswordChanged));
    }

    [Fact]
    public async Task Leo_can_reset_through_his_email()
    {
        await Request("pedrosa.leonardo@gmail.com");

        var sent = Assert.Single(_email.Resets);
        var result = await Reset(sent.UserId, sent.Token, "OutraSenha77");

        Assert.True(result.Succeeded);
        Assert.Equal(ReservedUsers.LeoId, sent.UserId);
        Assert.True(await CheckPassword("pedrosa.leonardo@gmail.com", "OutraSenha77"));
    }

    [Theory]
    [InlineData("ninguem@example.com")]
    [InlineData("pendente@example.com")]
    [InlineData("not an email")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Unknown_pending_or_invalid_emails_send_nothing_and_do_not_throw(string? email)
    {
        await Request(email);

        Assert.Empty(_email.Resets);
    }

    [Fact]
    public async Task Link_works_only_once()
    {
        await Request("ana@example.com");
        var sent = _email.Resets.Single();

        Assert.True((await Reset(sent.UserId, sent.Token, "NovaSenha99")).Succeeded);

        Assert.False(await IsValid(sent.UserId, sent.Token));
        var again = await Reset(sent.UserId, sent.Token, "TerceiraSenha1");
        Assert.True(again.InvalidLink);
        Assert.False(await CheckPassword("ana@example.com", "TerceiraSenha1"));
    }

    [Fact]
    public async Task Each_request_issues_a_new_working_link()
    {
        await Request("ana@example.com");
        var first = _email.Resets.Single();

        Assert.True((await Reset(first.UserId, first.Token, "NovaSenha99")).Succeeded);
        await Request("ana@example.com");
        var second = _email.Resets.Last();

        Assert.NotEqual(first.Token, second.Token);
        Assert.True(await IsValid(second.UserId, second.Token));
    }

    [Fact]
    public async Task Weak_password_is_rejected_in_portuguese_and_the_link_stays_valid()
    {
        await Request("ana@example.com");
        var sent = _email.Resets.Single();

        var weak = await Reset(sent.UserId, sent.Token, "curta1");

        Assert.False(weak.Succeeded);
        Assert.False(weak.InvalidLink);
        Assert.Contains(weak.Errors, e => e.Contains("pelo menos 8 caracteres"));
        Assert.True(await CheckPassword("ana@example.com", "Senha1234"));
        Assert.True((await Reset(sent.UserId, sent.Token, "NovaSenha99")).Succeeded);
    }

    [Fact]
    public async Task Garbage_or_foreign_tokens_are_invalid()
    {
        await Request("ana@example.com");
        var sent = _email.Resets.Single();

        Assert.True((await Reset(sent.UserId, "token-errado", "NovaSenha99")).InvalidLink);
        Assert.True((await Reset("nao-existe", sent.Token, "NovaSenha99")).InvalidLink);
        Assert.True((await Reset(null, null, "NovaSenha99")).InvalidLink);
        Assert.True((await Reset(ReservedUsers.LeoId, sent.Token, "NovaSenha99")).InvalidLink);
        Assert.False(await IsValid(sent.UserId, ""));
    }

    [Fact]
    public async Task Activation_token_cannot_reset_a_password()
    {
        string token;
        using (var scope = _root.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            token = await users.GenerateEmailConfirmationTokenAsync((await users.FindByIdAsync(_anaId))!);
        }

        Assert.True((await Reset(_anaId, token, "NovaSenha99")).InvalidLink);
    }

    [Fact]
    public async Task Pending_account_cannot_reset_even_with_a_valid_token()
    {
        string userId, token;
        using (var scope = _root.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var pending = (await users.FindByEmailAsync("pendente@example.com"))!;
            userId = pending.Id;
            token = await users.GeneratePasswordResetTokenAsync(pending);
        }

        Assert.True((await Reset(userId, token, "NovaSenha99")).InvalidLink);
        Assert.False(await CheckPassword("pendente@example.com", "NovaSenha99"));
    }

    [Fact]
    public async Task Reset_lifts_a_lockout()
    {
        using (var scope = _root.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var ana = (await users.FindByIdAsync(_anaId))!;
            await users.SetLockoutEndDateAsync(ana, DateTimeOffset.UtcNow.AddMinutes(15));
            Assert.True(await users.IsLockedOutAsync(ana));
        }

        await Request("ana@example.com");
        var sent = _email.Resets.Single();
        Assert.True((await Reset(sent.UserId, sent.Token, "NovaSenha99")).Succeeded);

        using var check = _root.CreateScope();
        var manager = check.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.False(await manager.IsLockedOutAsync((await manager.FindByIdAsync(_anaId))!));
    }

    [Fact]
    public void Reset_links_live_for_one_hour_and_activation_links_for_24()
    {
        var reset = _root.GetRequiredService<IOptions<PasswordResetTokenProviderOptions>>().Value;
        var other = _root.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value;

        Assert.Equal(TimeSpan.FromHours(1), reset.TokenLifespan);
        Assert.Equal(TimeSpan.FromHours(24), other.TokenLifespan);
    }

    [Fact]
    public async Task Email_limit_is_five_per_hour_and_hides_whether_the_account_exists()
    {
        for (var i = 0; i < 8; i++)
            await Request("ana@example.com", ip: null);
        for (var i = 0; i < 8; i++)
            await Request("ninguem@example.com", ip: null);

        Assert.Equal(PasswordResetLimiter.MaxPerEmail, _email.Resets.Count);
    }

    [Fact]
    public async Task Ip_limit_is_twenty_per_hour_across_addresses()
    {
        for (var i = 0; i < 30; i++)
            await Request($"nome{i}@example.com", "198.51.100.9");

        using var scope = _root.CreateScope();
        var limiter = scope.ServiceProvider.GetRequiredService<PasswordResetLimiter>();
        Assert.False(limiter.TryAcquire("ana@example.com", "198.51.100.9"));
        Assert.True(limiter.TryAcquire("ana@example.com", "198.51.100.10"));
    }

    [Fact]
    public void Sliding_window_limiter_releases_after_the_window_and_prunes_unused_keys()
    {
        var time = new ManualTime();
        var limiter = new SlidingWindowLimiter(time, 2, TimeSpan.FromHours(1));

        Assert.True(limiter.TryAcquire("a"));
        Assert.True(limiter.TryAcquire("A"));
        Assert.False(limiter.TryAcquire("a"));
        Assert.True(limiter.TryAcquire("b"));

        time.Advance(TimeSpan.FromHours(1));
        Assert.True(limiter.TryAcquire("a"));

        for (var i = 0; i < 6000; i++)
            limiter.TryAcquire("k" + i);
        time.Advance(TimeSpan.FromHours(2));
        Assert.True(limiter.TryAcquire("trigger-prune"));
        Assert.True(limiter.TryAcquire("k1"));
    }

    private sealed class ManualTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    private sealed class RecordingEmailSender : IAppEmailSender
    {
        public List<(string To, string UserId, string Token)> Resets { get; } = new();
        public List<string> PasswordChanged { get; } = new();

        public Task SendActivationAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendPasswordResetAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default)
        {
            Resets.Add((toAddress, userId, token));
            return Task.CompletedTask;
        }

        public Task SendPasswordChangedAsync(string toAddress, CancellationToken cancellationToken = default)
        {
            PasswordChanged.Add(toAddress);
            return Task.CompletedTask;
        }
    }
}
