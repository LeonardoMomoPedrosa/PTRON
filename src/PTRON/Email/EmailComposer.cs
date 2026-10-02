using Microsoft.Extensions.Options;

namespace PTRON.Email;

public sealed class EmailComposer
{
    private readonly AppOptions _app;

    public EmailComposer(IOptions<AppOptions> app)
    {
        _app = app.Value;
    }

    public EmailMessage Activation(string to, string userId, string token)
        => EmailTemplates.Activation(to, AppLinks.Activation(_app.PublicBaseUrl, userId, token), AppLinks.Logo(_app.PublicBaseUrl));

    public EmailMessage PasswordReset(string to, string userId, string token)
        => EmailTemplates.PasswordReset(to, AppLinks.PasswordReset(_app.PublicBaseUrl, userId, token), AppLinks.Logo(_app.PublicBaseUrl));

    public EmailMessage PasswordChanged(string to)
        => EmailTemplates.PasswordChanged(to, AppLinks.Login(_app.PublicBaseUrl), AppLinks.Logo(_app.PublicBaseUrl));
}
