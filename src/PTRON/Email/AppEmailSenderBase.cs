namespace PTRON.Email;

public abstract class AppEmailSenderBase : IAppEmailSender
{
    private readonly EmailComposer _composer;

    protected AppEmailSenderBase(EmailComposer composer)
    {
        _composer = composer;
    }

    public Task SendActivationAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default)
        => Deliver(_composer.Activation(toAddress, userId, token), cancellationToken);

    public Task SendPasswordResetAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default)
        => Deliver(_composer.PasswordReset(toAddress, userId, token), cancellationToken);

    public Task SendPasswordChangedAsync(string toAddress, CancellationToken cancellationToken = default)
        => Deliver(_composer.PasswordChanged(toAddress), cancellationToken);

    public Task SendProbeAsync(string toAddress, CancellationToken cancellationToken = default)
        => Deliver(
            new EmailMessage(
                toAddress,
                "Teste de e-mail Makelectron",
                "Este é um e-mail de teste do PTRON. Pode apagar.",
                "<p>Este é um e-mail de teste do PTRON. Pode apagar.</p>"),
            cancellationToken);

    protected abstract Task Deliver(EmailMessage message, CancellationToken cancellationToken);
}
