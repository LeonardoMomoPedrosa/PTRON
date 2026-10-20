using PTRON.Localization;
namespace PTRON.Services;

internal static class Validation
{
    public static string RequireName(string? nome)
    {
        var trimmed = (nome ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new InvalidOperationException(StaticText.Get("Informe o nome."));
        }

        return trimmed;
    }
}
