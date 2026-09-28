namespace PTRON.Services;

/// <summary>
/// Currency of a stock entry. Product prices, shipment and taxes are all
/// entered in this currency. Inventory cost is always stored in BRL.
/// </summary>
public static class Moedas
{
    public const string Brl = "BRL";
    public const string Usd = "USD";

    public static string Normalize(string? moeda)
    {
        if (string.Equals(moeda, Usd, StringComparison.OrdinalIgnoreCase))
        {
            return Usd;
        }

        if (string.Equals(moeda, Brl, StringComparison.OrdinalIgnoreCase))
        {
            return Brl;
        }

        return moeda?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    public static bool IsValid(string? moeda) => Normalize(moeda) is Brl or Usd;

    public static string Symbol(string? moeda) => Normalize(moeda) == Usd ? "US$" : "R$";
}
