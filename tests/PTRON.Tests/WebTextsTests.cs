using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using PTRON.Identity;
using PTRON.Localization;
using Xunit;

namespace PTRON.Tests;

public class WebTextsTests
{
    private static readonly ResourceManager Manager =
        new("PTRON.Resources.SharedResource", typeof(SharedResource).Assembly);

    private static readonly HashSet<string> SameInEveryLanguage = new()
    {
        "Makelectron", "Beta", "SQL", "Total", "Total (R$)", "Total {0}", "{first_item}-{last_item} de {all_items}", "Subtotal", "Status", "Add", "E-mail", "euro", "yuan", "real",
    };

    private static string RepoSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PTRON.sln")))
            dir = dir.Parent;
        return Path.Combine(dir!.FullName, "src", "PTRON");
    }

    private static IEnumerable<string> KeysUsedInCode()
    {
        var files = Directory.EnumerateFiles(RepoSource(), "*.*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".razor") || f.EndsWith(".cshtml") || f.EndsWith(".cs"))
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains("Migrations"));
        var pattern = new Regex(@"(?:\bL\[|StaticText\.(?:Get|Format)\(|ErrorMessage = )""((?:[^""\\]|\\.)*)""");
        foreach (var file in files)
            foreach (Match m in pattern.Matches(File.ReadAllText(file)))
                yield return m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }

    [Fact]
    public void Every_key_used_in_code_has_an_entry_in_each_language()
    {
        var missing = new List<string>();
        foreach (var key in KeysUsedInCode().Distinct())
            foreach (var lang in new[] { "pt-BR", "en", "es" })
            {
                var culture = CultureInfo.GetCultureInfo(lang);
                if (Manager.GetString(key, culture) is null) missing.Add($"{lang}: {key}");
            }

        Assert.True(missing.Count == 0, string.Join("\n", missing));
    }

    [Fact]
    public void English_and_spanish_are_translated_not_copied()
    {
        var en = CultureInfo.GetCultureInfo("en");
        var es = CultureInfo.GetCultureInfo("es");
        var untranslated = KeysUsedInCode().Distinct()
            .Where(k => !SameInEveryLanguage.Contains(k))
            .Where(k => Manager.GetString(k, en) == k || (k.Length > 25 && Manager.GetString(k, es) == k))
            .ToList();

        Assert.True(untranslated.Count == 0, string.Join("\n", untranslated));
    }

    [Fact]
    public void Placeholders_match_between_languages()
    {
        var placeholders = new Regex(@"\{\d+(:[^}]*)?\}");
        var bad = new List<string>();
        foreach (var key in KeysUsedInCode().Distinct())
            foreach (var lang in new[] { "en", "es" })
            {
                var text = Manager.GetString(key, CultureInfo.GetCultureInfo(lang)) ?? "";
                var a = placeholders.Matches(key).Select(m => m.Value).OrderBy(x => x);
                var b = placeholders.Matches(text).Select(m => m.Value).OrderBy(x => x);
                if (!a.SequenceEqual(b)) bad.Add($"{lang}: {key}");
            }

        Assert.True(bad.Count == 0, string.Join("\n", bad));
    }

    [Fact]
    public void Country_names_follow_the_language_and_brazil_stays_first()
    {
        var old = CultureInfo.CurrentUICulture;
        try
        {
            foreach (var (lang, name, germany) in new[]
                     { ("pt-BR", "Brasil", "Alemanha"), ("en", "Brazil", "Germany"), ("es", "Brasil", "Alemania") })
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
                Assert.Equal("BR", Paises.Todos[0].Codigo);
                Assert.Equal(name, Paises.Todos[0].Nome);
                Assert.Equal(germany, Paises.Nome("DE"));
                Assert.Equal(248, Paises.Todos.Skip(1).Count());
            }

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            var names = Paises.Todos.Skip(1).Select(p => p.Nome).ToList();
            Assert.Equal("Afghanistan", names[0]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = old;
        }
    }

    [Fact]
    public void Identity_errors_and_validation_messages_follow_the_language()
    {
        var old = CultureInfo.CurrentUICulture;
        try
        {
            var describer = new PortugueseIdentityErrorDescriber();
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            Assert.Equal("The password must have at least 8 characters.", describer.PasswordTooShort(8).Description);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es");
            Assert.Equal("La contraseña debe tener al menos 8 caracteres.", describer.PasswordTooShort(8).Description);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
            Assert.Equal("A senha deve ter pelo menos 8 caracteres.", describer.PasswordTooShort(8).Description);
        }
        finally
        {
            CultureInfo.CurrentUICulture = old;
        }
    }
}
