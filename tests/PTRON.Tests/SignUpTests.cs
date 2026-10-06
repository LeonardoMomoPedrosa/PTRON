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

public sealed class SignUpTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _root = null!;
    private readonly FakeEmailSender _email = new();

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddPtronIdentity(configuration);
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

    private static SignUpRequest Valid(string email = "maria@example.com") => new()
    {
        Email = email,
        Pais = "BR",
        Estado = "São Paulo",
        Senha = "Senha1234",
    };

    private async Task<SignUpResult> Register(SignUpRequest request)
    {
        using var scope = _root.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SignUpService>().RegisterAsync(request);
    }

    private async Task<ApplicationUser?> Find(string email)
    {
        using var scope = _root.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email);
    }

    [Fact]
    public async Task Creates_an_inactive_account_and_sends_the_activation_email()
    {
        var result = await Register(Valid());

        Assert.True(result.Succeeded);
        var user = await Find("maria@example.com");
        Assert.NotNull(user);
        Assert.False(user!.EmailConfirmed);
        Assert.False(user.Reservado);
        Assert.Equal("BR", user.Pais);
        Assert.Equal("São Paulo", user.Estado);
        Assert.NotNull(user.PasswordHash);

        var sent = Assert.Single(_email.Activations);
        Assert.Equal("maria@example.com", sent.To);
        Assert.Equal(user.Id, sent.UserId);

        using var scope = _root.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var confirmed = await users.ConfirmEmailAsync((await users.FindByIdAsync(user.Id))!, sent.Token);
        Assert.True(confirmed.Succeeded);
    }

    [Fact]
    public async Task Inactive_account_cannot_sign_in_with_the_password()
    {
        await Register(Valid());

        using var scope = _root.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByEmailAsync("maria@example.com"))!;

        Assert.True(await users.CheckPasswordAsync(user, "Senha1234"));
        Assert.False(await users.IsEmailConfirmedAsync(user));
    }

    [Fact]
    public async Task Existing_pending_email_gets_the_same_response_and_a_new_activation_link_without_a_new_account()
    {
        await Register(Valid());
        _email.Activations.Clear();

        var again = await Register(Valid("MARIA@example.com").With(r => r.Senha = "OutraSenha99"));

        Assert.True(again.Succeeded);
        Assert.Empty(again.Errors);
        var sent = Assert.Single(_email.Activations);
        Assert.Equal("maria@example.com", sent.To);
        using var scope = _root.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.CountAsync(u => u.Id != ReservedUsers.LeoId));

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByEmailAsync("maria@example.com"))!;
        Assert.True(await users.CheckPasswordAsync(user, "Senha1234"));
        Assert.False(await users.CheckPasswordAsync(user, "OutraSenha99"));
        Assert.True((await users.ConfirmEmailAsync(user, sent.Token)).Succeeded);
    }

    [Fact]
    public async Task Activated_email_gets_the_same_response_and_no_email()
    {
        await Register(Valid());
        using (var scope = _root.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync("maria@example.com"))!;
            await users.ConfirmEmailAsync(user, await users.GenerateEmailConfirmationTokenAsync(user));
        }
        _email.Activations.Clear();

        var again = await Register(Valid());

        Assert.True(again.Succeeded);
        Assert.Empty(again.Errors);
        Assert.Empty(_email.Activations);
    }

    [Fact]
    public async Task Activation_emails_are_limited_to_seven_per_address()
    {
        for (var i = 0; i < 10; i++)
        {
            var result = await Register(Valid());
            Assert.True(result.Succeeded);
        }

        Assert.Equal(ActivationEmailLimiter.DefaultMaxEmails, _email.Activations.Count);
        Assert.Equal(7, _email.Activations.Count);
    }

    [Fact]
    public void Limiter_releases_after_the_window_and_is_per_address()
    {
        var time = new ManualTime();
        var limiter = new ActivationEmailLimiter(time, 2, TimeSpan.FromHours(24));

        Assert.True(limiter.TryAcquire("a@example.com"));
        Assert.True(limiter.TryAcquire("A@example.com"));
        Assert.False(limiter.TryAcquire("a@example.com"));
        Assert.True(limiter.TryAcquire("b@example.com"));

        time.Advance(TimeSpan.FromHours(24));
        Assert.True(limiter.TryAcquire("a@example.com"));
    }

    [Theory]
    [InlineData("pedrosa.leonardo@gmail.com")]
    [InlineData("PEDROSA.LEONARDO@gmail.com")]
    public async Task Leo_email_is_blocked(string email)
    {
        var result = await Register(Valid(email));

        Assert.False(result.Succeeded);
        Assert.Contains("Este e-mail não está disponível.", result.Errors);
        Assert.Empty(_email.Activations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sem-arroba")]
    [InlineData("a@b")]
    [InlineData("Maria <maria@example.com>")]
    public async Task Invalid_email_is_rejected(string? email)
    {
        var result = await Register(Valid("x@example.com").With(r => r.Email = email));

        Assert.False(result.Succeeded);
        Assert.Contains("Informe um e-mail válido.", result.Errors);
        Assert.Empty(_email.Activations);
    }

    [Fact]
    public async Task Country_is_required_and_must_be_in_the_list()
    {
        var missing = await Register(Valid().With(r => r.Pais = ""));
        var unknown = await Register(Valid().With(r => r.Pais = "ZZ"));

        Assert.Contains("Selecione o país.", missing.Errors);
        Assert.Contains("Selecione o país.", unknown.Errors);
        Assert.Null(await Find("maria@example.com"));
    }

    [Fact]
    public async Task State_is_required_and_limited_to_100_characters()
    {
        var missing = await Register(Valid().With(r => r.Estado = "   "));
        var tooLong = await Register(Valid().With(r => r.Estado = new string('a', 101)));
        var max = await Register(Valid().With(r => r.Estado = new string('a', 100)));

        Assert.Contains("Informe o estado.", missing.Errors);
        Assert.Contains("O estado deve ter no máximo 100 caracteres.", tooLong.Errors);
        Assert.True(max.Succeeded);
    }

    [Theory]
    [InlineData("curta1", "pelo menos 8 caracteres")]
    [InlineData("somenteletras", "pelo menos um número")]
    [InlineData("12345678", "pelo menos uma letra")]
    [InlineData("", "Informe a senha.")]
    public async Task Password_policy_errors_are_in_portuguese(string password, string expected)
    {
        var result = await Register(Valid().With(r => r.Senha = password));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains(expected));
        Assert.Null(await Find("maria@example.com"));
        Assert.Empty(_email.Activations);
    }

    [Fact]
    public async Task Email_send_failure_does_not_fail_the_sign_up()
    {
        _email.Throw = true;

        var result = await Register(Valid());

        Assert.True(result.Succeeded);
        Assert.NotNull(await Find("maria@example.com"));
    }

    [Fact]
    public void Country_list_has_brazil_first_and_valid_unique_codes()
    {
        Assert.Equal("BR", Paises.Todos[0].Codigo);
        Assert.Equal("Brasil", Paises.Todos[0].Nome);
        Assert.Equal(Paises.Todos.Count, Paises.Todos.Select(p => p.Codigo).Distinct().Count());
        Assert.All(Paises.Todos, p => Assert.Matches("^[A-Z]{2}$", p.Codigo));
        Assert.True(Paises.IsValid("us"));
        Assert.False(Paises.IsValid("XX"));
        Assert.Equal("Portugal", Paises.Nome("PT"));
    }

    private sealed class ManualTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    private sealed class FakeEmailSender : IAppEmailSender
    {
        public List<(string To, string UserId, string Token)> Activations { get; } = new();
        public bool Throw { get; set; }

        public Task SendActivationAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default)
        {
            if (Throw) throw new InvalidOperationException("smtp down");
            Activations.Add((toAddress, userId, token));
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendPasswordChangedAsync(string toAddress, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

internal static class SignUpRequestExtensions
{
    public static SignUpRequest With(this SignUpRequest request, Action<SignUpRequest> change)
    {
        change(request);
        return request;
    }
}
