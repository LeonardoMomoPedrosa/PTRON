using System.Globalization;

namespace PTRON.Services;

/// <summary>
/// Formatting helpers using the Brazilian (pt-BR) culture.
/// </summary>
public static class Format
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Formats a value as Brazilian currency, e.g. "R$ 1.234,56".</summary>
    public static string Money(decimal value) => Money(value, Moedas.Brl);

    /// <summary>Formats a value in the entry currency (R$ or US$).</summary>
    public static string Money(decimal value, string? moeda)
    {
        if (Moedas.Normalize(moeda) == Moedas.Usd)
        {
            return "US$ " + value.ToString("N2", Culture);
        }

        return value.ToString("C", Culture);
    }

    public static string Percent(decimal ratio) => ratio.ToString("P2", Culture);

    /// <summary>Formats a number with up to 4 decimals, trimming trailing zeros.</summary>
    public static string Number(decimal value) => value.ToString("0.####", Culture);

    /// <summary>Tipo, valor, potência and voltagem of an insumo, joined by " · " (empty when none is set).</summary>
    public static string InsumoDetalhe(Models.Insumo insumo)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(insumo.TipoInsumo?.Nome)) parts.Add(insumo.TipoInsumo.Nome);
        if (!string.IsNullOrWhiteSpace(insumo.Valor)) parts.Add(insumo.Valor!);
        if (!string.IsNullOrWhiteSpace(insumo.Potencia)) parts.Add(insumo.Potencia!);
        if (!string.IsNullOrWhiteSpace(insumo.Voltagem)) parts.Add(insumo.Voltagem!);
        return string.Join(" · ", parts);
    }

    public static string Date(DateTime value) => value.ToString("dd/MM/yyyy HH:mm", Culture);
}
