using System.Globalization;

namespace PTRON.Services;

/// <summary>
/// Formatting helpers. Numbers and dates follow the active culture (pt-BR, en or es);
/// the currency symbol does not change with the language.
/// </summary>
public static class Format
{
    public static CultureInfo Culture => CultureInfo.CurrentCulture;

    /// <summary>Formats a value as Brazilian reais, e.g. "R$ 1.234,56" (pt-BR, es) or "R$ 1,234.56" (en).</summary>
    public static string Money(decimal value) => Money(value, Moedas.Brl);

    /// <summary>Formats a value in the given currency (R$, US$, €, £ or CN¥).</summary>
    public static string Money(decimal value, string? moeda)
    {
        var code = Moedas.Normalize(moeda);
        if (code == Moedas.Brl || string.IsNullOrEmpty(code))
        {
            return Moedas.Symbol(Moedas.Brl) + " " + value.ToString("N2", Culture);
        }

        return Moedas.Symbol(code) + " " + value.ToString("N2", Culture);
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

    public static string Date(DateTime value) => value.ToString("g", Culture);
}
