namespace PTRON.Identity;

public static class ClientIp
{
    /// <summary>
    /// Client address behind the proxy (Cloudflare, then X-Forwarded-For). The headers can be forged by
    /// anyone who reaches the app directly, so this is only good for rate limiting, never for authorization.
    /// </summary>
    public static string? From(HttpContext context)
    {
        var cloudflare = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(cloudflare))
            return cloudflare.Trim();

        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var first = forwarded?.Split(',')[0].Trim();
        if (!string.IsNullOrEmpty(first))
            return first;

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
