namespace PTRON.Email;

public static class AppLinks
{
    public static string Activation(string? baseUrl, string userId, string token)
        => Combine(baseUrl, "/ativar?userId=" + Esc(userId) + "&token=" + Esc(token));

    public static string PasswordReset(string? baseUrl, string userId, string token)
        => Combine(baseUrl, "/redefinir-senha?userId=" + Esc(userId) + "&token=" + Esc(token));

    public static string Login(string? baseUrl)
        => Combine(baseUrl, "/login");

    public static string Logo(string? baseUrl)
        => string.IsNullOrWhiteSpace(baseUrl) ? "" : Combine(baseUrl, "/logo.png");

    public static string Combine(string? baseUrl, string relative)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return relative;
        }

        return baseUrl.TrimEnd('/') + "/" + relative.TrimStart('/');
    }

    private static string Esc(string value) => Uri.EscapeDataString(value ?? "");
}
