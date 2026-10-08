namespace PTRON.Services;

/// <summary>
/// Currencies of a stock entry. Product prices, shipment and taxes may each use
/// one of these codes. Inventory cost is always stored in <see cref="Destino"/> (BRL).
/// </summary>
public static class Moedas
{
    public const string Brl = "BRL";
    public const string Usd = "USD";
    public const string Eur = "EUR";
    public const string Gbp = "GBP";
    public const string Cny = "CNY";

    /// <summary>Currency in which the landed unit cost is calculated and stored.</summary>
    public const string Destino = Brl;

    public static readonly IReadOnlyList<string> Todas = new[] { Brl, Usd, Eur, Gbp, Cny };

    public static string Normalize(string? moeda)
    {
        var code = moeda?.Trim().ToUpperInvariant() ?? string.Empty;
        return Todas.Contains(code) ? code : code;
    }

    public static bool IsValid(string? moeda) => Todas.Contains(Normalize(moeda));

    public static string Symbol(string? moeda) => Normalize(moeda) switch
    {
        Usd => "US$",
        Eur => "€",
        Gbp => "£",
        Cny => "CN¥",
        _ => "R$"
    };

    /// <summary>Singular name in Portuguese, used in exchange-rate hints.</summary>
    public static string Nome(string? moeda) => Normalize(moeda) switch
    {
        Usd => "dólar",
        Eur => "euro",
        Gbp => "libra",
        Cny => "yuan",
        _ => "real"
    };

    /// <summary>How many reais one unit of <paramref name="moeda"/> is worth. Always 1 for BRL.</summary>
    public static decimal CambioParaDestino(string? moeda, decimal informado)
        => Normalize(moeda) == Destino ? 1m : informado;

    /// <summary>
    /// Exchange rate of the freight currency into reais. Freight must be priced in the
    /// product currency, the tax currency, or reais.
    /// </summary>
    public static decimal CambioFrete(
        string? moedaFrete,
        string? moedaProdutos,
        decimal cambioProdutos,
        string? moedaImpostos,
        decimal cambioImpostos)
    {
        var frete = Normalize(moedaFrete);
        if (frete == Destino)
        {
            return 1m;
        }

        if (frete == Normalize(moedaProdutos))
        {
            return CambioParaDestino(moedaProdutos, cambioProdutos);
        }

        if (frete == Normalize(moedaImpostos))
        {
            return CambioParaDestino(moedaImpostos, cambioImpostos);
        }

        return 0m;
    }
}
