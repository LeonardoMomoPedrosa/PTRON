using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PTRON.Email;
using Xunit;

namespace PTRON.Tests;

public sealed class EmailSenderTests
{
    [Fact]
    public void Activation_link_uses_the_public_base_url_and_encodes_the_token()
    {
        var url = AppLinks.Activation("https://makelectron.com/", "user 1", "a+b/c=");

        Assert.Equal("https://makelectron.com/ativar?userId=user%201&token=a%2Bb%2Fc%3D", url);
    }

    [Fact]
    public void Templates_are_portuguese_and_use_the_makelectron_colors()
    {
        var activation = EmailTemplates.Activation("ana@example.com", "https://makelectron.com/ativar?userId=1&token=abc", "https://makelectron.com/logo.png");
        Assert.Equal("Ative sua conta no Makelectron", activation.Subject);
        Assert.Contains("24 horas", activation.TextBody);
        Assert.Contains("https://makelectron.com/ativar?userId=1&token=abc", activation.TextBody);
        Assert.Contains(EmailTemplates.Navy, activation.HtmlBody);
        Assert.Contains(EmailTemplates.Blue, activation.HtmlBody);
        Assert.Contains("logo.png", activation.HtmlBody);

        var reset = EmailTemplates.PasswordReset("ana@example.com", "https://makelectron.com/redefinir-senha?userId=1&token=abc", null);
        Assert.Contains("1 hora", reset.TextBody);
        Assert.Contains("MAKE", reset.HtmlBody);

        var changed = EmailTemplates.PasswordChanged("ana@example.com", "https://makelectron.com/login", null);
        Assert.Contains("foi alterada", changed.Subject);
        Assert.Contains("Esqueci a senha", changed.TextBody);
    }

    [Fact]
    public async Task Development_log_includes_the_activation_link()
    {
        var logger = new ListLogger<LoggingAppEmailSender>();
        var sender = new LoggingAppEmailSender(Composer(), logger, new Env("Development"));

        await sender.SendActivationAsync("ana@example.com", "ana", "tok+en");

        var message = Assert.Single(logger.Messages);
        Assert.Contains("ana@example.com", message);
        Assert.Contains("https://makelectron.com/ativar?userId=ana&token=tok%2Ben", message);
    }

    [Fact]
    public async Task Production_without_smtp_does_not_log_the_token()
    {
        var logger = new ListLogger<LoggingAppEmailSender>();
        var sender = new LoggingAppEmailSender(Composer(), logger, new Env("Production"));

        await sender.SendPasswordResetAsync("ana@example.com", "ana", "segredo");

        var message = Assert.Single(logger.Messages);
        Assert.Contains("não enviado", message);
        Assert.DoesNotContain("segredo", message);
    }

    [Fact]
    public async Task Smtp_failure_is_logged_and_does_not_throw()
    {
        var logger = new ListLogger<SmtpAppEmailSender>();
        var smtp = Options.Create(new SmtpOptions
        {
            Host = "127.0.0.1",
            Port = 1,
            FromAddress = "nao-responda@makelectron.com",
            FromName = "Makelectron",
            SecureSocketOptions = "None"
        });
        var sender = new SmtpAppEmailSender(Composer(), smtp, logger);

        await sender.SendPasswordChangedAsync("ana@example.com");

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }

    private static EmailComposer Composer()
        => new(Options.Create(new AppOptions { PublicBaseUrl = "https://makelectron.com" }));

    private sealed class Env : IHostEnvironment
    {
        public Env(string name) => EnvironmentName = name;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "PTRON.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();
        public List<string> Messages => Entries.Select(e => e.Message).ToList();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
