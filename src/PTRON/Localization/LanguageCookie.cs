using Microsoft.AspNetCore.Localization;

namespace PTRON.Localization;

public static class LanguageCookie
{
    /// <summary>Stores the language in the culture cookie. Unsupported codes are ignored.</summary>
    public static void Write(HttpContext http, string? code)
    {
        var language = SupportedLanguages.Find(code);
        if (language is null) return;

        http.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language.Code)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = http.Request.IsHttps
            });
    }
}
