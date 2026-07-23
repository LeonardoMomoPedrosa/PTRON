namespace PTRON.Services;

internal static class Validation
{
    public static string RequireName(string? nome, string campo = "nome")
    {
        var trimmed = (nome ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new InvalidOperationException($"Informe o {campo}.");
        }

        return trimmed;
    }
}
