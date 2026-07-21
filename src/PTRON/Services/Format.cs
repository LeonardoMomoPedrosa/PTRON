using System.Globalization;

namespace PTRON.Services;

/// <summary>
/// Formatting helpers using the Brazilian (pt-BR) culture.
/// </summary>
public static class Format
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Formats a value as Brazilian currency, e.g. "R$ 1.234,56".</summary>
    public static string Money(decimal value) => value.ToString("C", Culture);

    /// <summary>Formats a number with up to 4 decimals, trimming trailing zeros.</summary>
    public static string Number(decimal value) => value.ToString("0.####", Culture);

    public static string Date(DateTime value) => value.ToString("dd/MM/yyyy HH:mm", Culture);
}
