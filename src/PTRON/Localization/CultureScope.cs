using System.Globalization;

namespace PTRON.Localization;

/// <summary>Runs a block under a given language (for example an account's language) and restores the previous one.</summary>
public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _culture;
    private readonly CultureInfo _uiCulture;

    private CultureScope(CultureInfo culture, CultureInfo uiCulture)
    {
        _culture = culture;
        _uiCulture = uiCulture;
    }

    public static CultureScope Use(string? languageCode)
    {
        var scope = new CultureScope(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        var language = SupportedLanguages.Find(languageCode);
        if (language is not null)
        {
            CultureInfo.CurrentCulture = language.Culture;
            CultureInfo.CurrentUICulture = language.Culture;
        }

        return scope;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}
