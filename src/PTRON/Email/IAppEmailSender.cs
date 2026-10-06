namespace PTRON.Email;

public interface IAppEmailSender
{
    Task SendActivationAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(string toAddress, string userId, string token, CancellationToken cancellationToken = default);

    Task SendPasswordChangedAsync(string toAddress, CancellationToken cancellationToken = default);
}
