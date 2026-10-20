using System.Globalization;

namespace PTRON.Localization;

public sealed record Language(string Code, string Name)
{
    public CultureInfo Culture => CultureInfo.GetCultureInfo(Code);
}

public static class SupportedLanguages
{
    public const string Default = "pt-BR";

    public static readonly IReadOnlyList<Language> All =
    [
        new("pt-BR", "Português"),
        new("en", "English"),
        new("es", "Español"),
    ];

    public static readonly CultureInfo[] Cultures = All.Select(l => l.Culture).ToArray();

    /// <summary>Returns the supported language matching the culture (or its parent), or null.</summary>
    public static Language? Find(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var match = All.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
        if (match is not null) return match;
        var neutral = code.Split('-')[0];
        return All.FirstOrDefault(l => string.Equals(l.Code.Split('-')[0], neutral, StringComparison.OrdinalIgnoreCase));
    }

    public static Language Current => Find(CultureInfo.CurrentUICulture.Name) ?? All[0];
}
