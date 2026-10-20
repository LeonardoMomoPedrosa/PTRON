using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using PTRON;
using PTRON.Localization;
using PTRON.Services;
using Xunit;

namespace PTRON.Tests;

public class LocalizationFoundationTests
{
    private static T WithCulture<T>(string name, Func<T> action)
    {
        var old = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            var culture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            return action();
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = old;
        }
    }

    [Theory]
    [InlineData("pt-BR", "R$ 1.234,56")]
    [InlineData("es", "R$ 1.234,56")]
    [InlineData("en", "R$ 1,234.56")]
    public void MoneyKeepsRealSymbolAndFollowsCultureSeparators(string culture, string expected)
    {
        Assert.Equal(expected, WithCulture(culture, () => Format.Money(1234.56m)).Replace('\u00a0', ' '));
    }

    [Fact]
    public void NumberAndDateFollowActiveCulture()
    {
        var date = new DateTime(2026, 10, 20, 14, 30, 0);
        Assert.Equal("1,5", WithCulture("pt-BR", () => Format.Number(1.5m)));
        Assert.Equal("1.5", WithCulture("en", () => Format.Number(1.5m)));
        Assert.Equal("20/10/2026 14:30", WithCulture("pt-BR", () => Format.Date(date)));
        Assert.StartsWith("10/20/2026", WithCulture("en", () => Format.Date(date)));
    }

    [Fact]
    public void SameKeyDiffersAcrossTheThreeCultures()
    {
        var provider = new ServiceCollection()
            .AddLogging()
            .AddLocalization(o => o.ResourcesPath = "Resources")
            .BuildServiceProvider();
        var localizer = provider.GetRequiredService<IStringLocalizer<SharedResource>>();

        var texts = SupportedLanguages.All
            .Select(l => WithCulture(l.Code, () => localizer["Escolher idioma"].Value))
            .ToArray();

        Assert.Equal(3, texts.Distinct().Count());
    }

    [Theory]
    [InlineData("en-US", "en")]
    [InlineData("es-MX", "es")]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("fr", null)]
    public void FindMatchesSupportedLanguageOrParent(string code, string? expected)
    {
        Assert.Equal(expected, SupportedLanguages.Find(code)?.Code);
    }
}
