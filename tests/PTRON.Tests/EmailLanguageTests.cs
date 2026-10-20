using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PTRON.Data;
using PTRON.Email;
using PTRON.Identity;
using PTRON.Localization;
using PTRON.Models;
using Xunit;

namespace PTRON.Tests;

public sealed class EmailLanguageTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _root = null!;
    private readonly CultureRecordingSender _email = new();

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

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

    [Theory]
    [InlineData("pt-BR", "Ative sua conta no Makelectron", "Sua senha foi alterada")]
    [InlineData("en", "Activate your Makelectron account", "Your password was changed")]
    [InlineData("es", "Activa tu cuenta de Makelectron", "Tu contraseña fue cambiada")]
    public void Templates_are_written_in_the_active_language(string language, string activationSubject, string changedHeading)
    {
        using var _ = CultureScope.Use(language);

        var activation = EmailTemplates.Activation("ana@example.com", "https://x/ativar", null);
        var changed = EmailTemplates.PasswordChanged("ana@example.com", "https://x/login", null);

        Assert.Equal(activationSubject, activation.Subject);
        Assert.Contains($"<html lang=\"{language}\">", activation.HtmlBody);
        Assert.Contains(changedHeading, changed.TextBody);
    }

    [Fact]
    public void Every_template_text_differs_between_languages()
    {
        var subjects = new[] { "pt-BR", "en", "es" }.Select(l =>
        {
            using var _ = CultureScope.Use(l);
            return EmailTemplates.PasswordReset("a@b.c", "https://x", null).Subject;
        }).ToArray();

        Assert.Equal(3, subjects.Distinct().Count());
    }

    [Fact]
    public async Task Resent_activation_uses_the_account_language()
    {
        string id;
        using (var scope = _root.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var ana = new ApplicationUser { UserName = "ana@example.com", Email = "ana@example.com", Idioma = "es" };
            Assert.True((await users.CreateAsync(ana, "Senha1234")).Succeeded);
            id = ana.Id;
        }

        using (var _ = CultureScope.Use("en"))
        {
            using var scope = _root.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ActivationService>().ResendAsync("ana@example.com");
            Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
        }

        Assert.Equal("es", _email.ActivationCulture);
    }

    private sealed class CultureRecordingSender : IAppEmailSender
    {
        public string? ActivationCulture { get; private set; }

        public Task SendActivationAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default)
        {
            ActivationCulture = CultureInfo.CurrentUICulture.Name;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendPasswordChangedAsync(string toAddress, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
