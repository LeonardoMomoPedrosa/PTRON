using System.Net;

namespace PTRON.Email;

public static class EmailTemplates
{
    public const string Navy = "#01295A";
    public const string Blue = "#007AFC";

    public static EmailMessage Activation(string to, string url, string? logoUrl)
        => Build(
            to,
            "Ative sua conta no Makelectron",
            "Ative sua conta",
            "Confirme seu e-mail para começar a usar o Makelectron. Este link vale por 24 horas.",
            "Ativar conta",
            url,
            "Se você não criou esta conta, ignore esta mensagem.",
            logoUrl);

    public static EmailMessage PasswordReset(string to, string url, string? logoUrl)
        => Build(
            to,
            "Redefina sua senha no Makelectron",
            "Redefina sua senha",
            "Recebemos um pedido para redefinir a senha da sua conta. Este link vale por 1 hora e só pode ser usado uma vez.",
            "Redefinir senha",
            url,
            "Se você não pediu esta alteração, ignore esta mensagem. Sua senha continua a mesma.",
            logoUrl);

    public static EmailMessage PasswordChanged(string to, string loginUrl, string? logoUrl)
        => Build(
            to,
            "Sua senha do Makelectron foi alterada",
            "Sua senha foi alterada",
            "A senha da sua conta Makelectron foi alterada agora.",
            "Ir para o login",
            loginUrl,
            "Se não foi você, use \"Esqueci a senha\" na tela de entrada.",
            logoUrl);

    private static EmailMessage Build(
        string to,
        string subject,
        string heading,
        string intro,
        string button,
        string url,
        string footnote,
        string? logoUrl)
    {
        var safeUrl = WebUtility.HtmlEncode(url);
        var text = $"""
            {heading}

            {intro}

            {url}

            {footnote}
            — Makelectron
            """;

        var brand = string.IsNullOrWhiteSpace(logoUrl)
            ? $"""<div style="font-size:22px;font-weight:700;letter-spacing:0.04em;color:{Navy};">MAKE<span style="color:{Blue};">LECTRON</span></div>"""
            : $"""<img src="{WebUtility.HtmlEncode(logoUrl)}" alt="Makelectron" height="48" style="height:48px;border:0;" />""";

        var html = $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <body style="margin:0;padding:0;background:#f4f7fb;font-family:Segoe UI,Arial,sans-serif;color:{Navy};">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f4f7fb;">
                <tr><td align="center" style="padding:32px 16px;">
                  <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;background:#ffffff;border-radius:8px;">
                    <tr><td style="height:4px;background:{Blue};border-radius:8px 8px 0 0;font-size:0;line-height:0;">&nbsp;</td></tr>
                    <tr><td align="center" style="padding:28px 32px 8px;">{brand}</td></tr>
                    <tr><td style="padding:8px 32px 32px;">
                      <h1 style="margin:0 0 16px;font-size:22px;line-height:1.3;color:{Navy};">{WebUtility.HtmlEncode(heading)}</h1>
                      <p style="margin:0 0 20px;font-size:16px;line-height:1.5;color:#24344a;">{WebUtility.HtmlEncode(intro)}</p>
                      <p style="margin:0 0 24px;">
                        <a href="{safeUrl}" style="display:inline-block;background:{Navy};color:#ffffff;text-decoration:none;font-weight:600;padding:12px 22px;border-radius:4px;">{WebUtility.HtmlEncode(button)}</a>
                      </p>
                      <p style="margin:0 0 8px;font-size:13px;line-height:1.5;color:#5c6b80;">Se o botão não funcionar, copie este endereço no navegador:</p>
                      <p style="margin:0 0 20px;font-size:13px;line-height:1.5;word-break:break-all;"><a href="{safeUrl}" style="color:{Blue};">{safeUrl}</a></p>
                      <p style="margin:0;font-size:13px;line-height:1.5;color:#5c6b80;">{WebUtility.HtmlEncode(footnote)}</p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        return new EmailMessage(to, subject, text, html);
    }
}
