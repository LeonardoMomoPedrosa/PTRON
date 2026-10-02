using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace PTRON.Email;

public sealed class SmtpAppEmailSender : AppEmailSenderBase
{
    private readonly IOptions<SmtpOptions> _smtp;
    private readonly ILogger<SmtpAppEmailSender> _logger;

    public SmtpAppEmailSender(EmailComposer composer, IOptions<SmtpOptions> smtp, ILogger<SmtpAppEmailSender> logger)
        : base(composer)
    {
        _smtp = smtp;
        _logger = logger;
    }

    protected override async Task Deliver(EmailMessage message, CancellationToken cancellationToken)
    {
        var smtp = _smtp.Value;
        try
        {
            if (!smtp.IsConfigured)
            {
                _logger.LogError("E-mail para {To} não enviado: SMTP incompleto. Assunto: {Subject}", message.To, message.Subject);
                return;
            }

            if (!TryParseSecurity(smtp.SecureSocketOptions, out var security))
            {
                _logger.LogError(
                    "E-mail para {To} não enviado: Smtp:SecureSocketOptions inválido ({Value}).",
                    message.To,
                    smtp.SecureSocketOptions);
                return;
            }

            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress!));
            mime.To.Add(MailboxAddress.Parse(message.To));
            mime.Subject = message.Subject;
            mime.Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody();

            using var client = new SmtpClient { Timeout = 15_000 };
            await client.ConnectAsync(smtp.Host!, smtp.Port, security, cancellationToken);
            if (!string.IsNullOrWhiteSpace(smtp.User))
            {
                await client.AuthenticateAsync(smtp.User, smtp.Password ?? "", cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {To}. Assunto: {Subject}", message.To, message.Subject);
        }
    }

    internal static bool TryParseSecurity(string? value, out SecureSocketOptions security)
    {
        if (Enum.TryParse(value, ignoreCase: true, out security) && Enum.IsDefined(security))
        {
            return true;
        }

        security = SecureSocketOptions.StartTls;
        return false;
    }
}
