using Microsoft.AspNetCore.Http;
using PTRON.Localization;
using Xunit;

namespace PTRON.Tests;

public class LanguageCookieTests
{
    [Fact]
    public void Writes_the_culture_cookie_for_a_supported_language()
    {
        var http = new DefaultHttpContext();
        LanguageCookie.Write(http, "es");

        var header = http.Response.Headers.SetCookie.ToString();
        Assert.Contains(".AspNetCore.Culture=c%3Des%7Cuic%3Des", header);
    }

    [Fact]
    public void Ignores_unsupported_languages()
    {
        var http = new DefaultHttpContext();
        LanguageCookie.Write(http, "fr");

        Assert.Equal("", http.Response.Headers.SetCookie.ToString());
    }
}
