namespace PTRON.Email;

/// <summary>
/// Used when SMTP is not configured. In development the message, including the link,
/// is written to the log. In production only a warning is written, without the link.
/// </summary>
public sealed class LoggingAppEmailSender : AppEmailSenderBase
{
    private readonly ILogger<LoggingAppEmailSender> _logger;
    private readonly IHostEnvironment _environment;

    public LoggingAppEmailSender(
        EmailComposer composer,
        ILogger<LoggingAppEmailSender> logger,
        IHostEnvironment environment)
        : base(composer)
    {
        _logger = logger;
        _environment = environment;
    }

    protected override Task Deliver(EmailMessage message, CancellationToken cancellationToken)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "E-mail de desenvolvimento para {To}. Assunto: {Subject}{NewLine}{Body}",
                message.To,
                message.Subject,
                Environment.NewLine,
                message.TextBody);
        }
        else
        {
            _logger.LogWarning(
                "E-mail para {To} não enviado: SMTP não configurado. Assunto: {Subject}",
                message.To,
                message.Subject);
        }

        return Task.CompletedTask;
    }
}
