namespace PTRON.Services;

public sealed class EntradaLinhaInput
{
    public int InsumoId { get; init; }
    public decimal Qtd { get; init; }
    public decimal PrecoUnitario { get; init; }
}

public sealed class EntradaCommand
{
    public string MoedaProdutos { get; init; } = Moedas.Brl;

    /// <summary>Reais per 1 unit of <see cref="MoedaProdutos"/>. Ignored when products are already in BRL.</summary>
    public decimal CambioProdutos { get; init; }

    public string MoedaImpostos { get; init; } = Moedas.Brl;

    /// <summary>
    /// Reais per 1 unit of <see cref="MoedaImpostos"/>. Ignored when taxes are in BRL
    /// or in the same currency as the products (that rate is reused).
    /// </summary>
    public decimal CambioImpostos { get; init; }

    /// <summary>Must be the product currency, the tax currency, or BRL. Defaults to the product currency.</summary>
    public string MoedaFrete { get; init; } = string.Empty;

    public decimal Frete { get; init; }
    public decimal Impostos { get; init; }
    public IReadOnlyList<EntradaLinhaInput> Itens { get; init; } = Array.Empty<EntradaLinhaInput>();
}

public sealed class EntradaRateioLinha
{
    public int InsumoId { get; init; }
    public decimal Qtd { get; init; }

    /// <summary>Unit price in the product currency, before shipment and taxes.</summary>
    public decimal PrecoUnitario { get; init; }

    /// <summary>Product subtotal in the product currency (qty × unit price).</summary>
    public decimal Subtotal { get; init; }

    /// <summary>Share of the product total, from 0 to 1. Shipment and taxes are excluded.</summary>
    public decimal Proporcao { get; init; }

    /// <summary>Shipment allocated to this line, in the freight currency.</summary>
    public decimal FreteRateado { get; init; }

    /// <summary>Taxes allocated to this line, in the tax currency.</summary>
    public decimal ImpostoRateado { get; init; }

    /// <summary>
    /// Landed unit cost in BRL. Rounded to 4 decimal places, which is the value
    /// applied to the weighted-average stock cost.
    /// </summary>
    public decimal CustoUnitarioBrl { get; init; }

    /// <summary>Landed line total in BRL, before unit-cost rounding.</summary>
    public decimal SubtotalBrl { get; init; }
}

public sealed class EntradaRateio
{
    public string MoedaProdutos { get; init; } = Moedas.Brl;
    public decimal CambioProdutos { get; init; } = 1m;
    public string MoedaImpostos { get; init; } = Moedas.Brl;
    public decimal CambioImpostos { get; init; } = 1m;
    public string MoedaFrete { get; init; } = Moedas.Brl;
    public decimal CambioFrete { get; init; } = 1m;
    public string MoedaDestino { get; init; } = Moedas.Destino;
    public decimal Frete { get; init; }
    public decimal Impostos { get; init; }
    public decimal TotalProdutos { get; init; }
    public IReadOnlyList<EntradaRateioLinha> Linhas { get; init; } = Array.Empty<EntradaRateioLinha>();

    public bool MoedasIguais =>
        MoedaProdutos == MoedaImpostos && MoedaProdutos == MoedaFrete;

    /// <summary>Sum of products, shipment and taxes. Meaningful only when <see cref="MoedasIguais"/>.</summary>
    public decimal TotalMoeda => TotalProdutos + Frete + Impostos;

    public decimal TotalBrl => Linhas.Sum(l => l.SubtotalBrl);
}

/// <summary>
/// Splits shipment and taxes across the products of a stock entry and converts
/// each amount into BRL with its own exchange rate.
/// Proportions use product subtotals only. The last line absorbs rounding
/// so the allocated amounts add back to the informed shipment and taxes,
/// each in its own currency.
/// </summary>
public static class EntradaCustoCalculator
{
    public const int Scale = 4;

    public static decimal RoundMoney(decimal value)
        => decimal.Round(value, Scale, MidpointRounding.AwayFromZero);

    public static EntradaRateio Calcular(EntradaCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var moedaProdutos = Moedas.Normalize(command.MoedaProdutos);
        var moedaImpostos = Moedas.Normalize(command.MoedaImpostos);
        var moedaFrete = Moedas.Normalize(
            string.IsNullOrWhiteSpace(command.MoedaFrete) ? moedaProdutos : command.MoedaFrete);

        if (!Moedas.IsValid(moedaProdutos))
        {
            throw new InvalidOperationException("Selecione a moeda dos produtos.");
        }

        if (!Moedas.IsValid(moedaImpostos))
        {
            throw new InvalidOperationException("Selecione a moeda dos impostos.");
        }

        if (moedaFrete != moedaProdutos && moedaFrete != moedaImpostos && moedaFrete != Moedas.Destino)
        {
            throw new InvalidOperationException(
                "O frete deve estar na moeda dos produtos, dos impostos ou em reais.");
        }

        var cambioProdutos = ResolveCambio(moedaProdutos, command.CambioProdutos, "produtos");
        var cambioImpostos = moedaImpostos == moedaProdutos
            ? cambioProdutos
            : ResolveCambio(moedaImpostos, command.CambioImpostos, "impostos");
        var cambioFrete = Moedas.CambioFrete(
            moedaFrete, moedaProdutos, cambioProdutos, moedaImpostos, cambioImpostos);

        if (command.Frete < 0)
        {
            throw new InvalidOperationException("O frete não pode ser negativo.");
        }

        if (command.Impostos < 0)
        {
            throw new InvalidOperationException("Os impostos não podem ser negativos.");
        }

        var frete = RoundMoney(command.Frete);
        var impostos = RoundMoney(command.Impostos);

        if (command.Itens is null || command.Itens.Count == 0)
        {
            throw new InvalidOperationException("Adicione ao menos um item à entrada.");
        }

        if (command.Itens.Any(i => i.InsumoId <= 0 || i.Qtd <= 0 || i.PrecoUnitario < 0))
        {
            throw new InvalidOperationException(
                "Todos os itens devem ter insumo, quantidade maior que zero e preço não negativo.");
        }

        var agregados = command.Itens
            .GroupBy(i => i.InsumoId)
            .Select(g =>
            {
                var qtdExata = g.Sum(x => x.Qtd);
                return new EntradaLinhaInput
                {
                    InsumoId = g.Key,
                    Qtd = RoundMoney(qtdExata),
                    PrecoUnitario = RoundMoney(g.Sum(x => x.Qtd * x.PrecoUnitario) / qtdExata)
                };
            })
            .ToList();

        var totalProdutos = agregados.Sum(i => i.Qtd * i.PrecoUnitario);
        var totalQtd = agregados.Sum(i => i.Qtd);
        var freteRestante = frete;
        var impostoRestante = impostos;
        var linhas = new List<EntradaRateioLinha>(agregados.Count);

        for (var i = 0; i < agregados.Count; i++)
        {
            var item = agregados[i];
            var subtotal = item.Qtd * item.PrecoUnitario;
            var proporcao = Proporcao(subtotal, totalProdutos, item.Qtd, totalQtd);
            var last = i == agregados.Count - 1;
            var freteRateado = TakeShare(proporcao, frete, ref freteRestante, last);
            var impostoRateado = TakeShare(proporcao, impostos, ref impostoRestante, last);
            var subtotalBrl = subtotal * cambioProdutos
                + freteRateado * cambioFrete
                + impostoRateado * cambioImpostos;
            var custoUnitario = item.Qtd == 0 ? 0m : RoundMoney(subtotalBrl / item.Qtd);

            linhas.Add(new EntradaRateioLinha
            {
                InsumoId = item.InsumoId,
                Qtd = item.Qtd,
                PrecoUnitario = item.PrecoUnitario,
                Subtotal = subtotal,
                Proporcao = proporcao,
                FreteRateado = freteRateado,
                ImpostoRateado = impostoRateado,
                CustoUnitarioBrl = custoUnitario,
                SubtotalBrl = subtotalBrl
            });
        }

        return new EntradaRateio
        {
            MoedaProdutos = moedaProdutos,
            CambioProdutos = cambioProdutos,
            MoedaImpostos = moedaImpostos,
            CambioImpostos = cambioImpostos,
            MoedaFrete = moedaFrete,
            CambioFrete = cambioFrete,
            MoedaDestino = Moedas.Destino,
            Frete = frete,
            Impostos = impostos,
            TotalProdutos = totalProdutos,
            Linhas = linhas
        };
    }

    /// <summary>
    /// Share of the product total. When every product price is zero, the share
    /// follows quantity so shipment and taxes can still be distributed.
    /// </summary>
    public static decimal Proporcao(decimal subtotal, decimal totalProdutos, decimal qtd, decimal totalQtd)
    {
        if (totalProdutos > 0)
        {
            return subtotal / totalProdutos;
        }

        if (totalQtd > 0)
        {
            return qtd / totalQtd;
        }

        return 0m;
    }

    private static decimal ResolveCambio(string moeda, decimal informado, string papel)
    {
        if (moeda == Moedas.Destino)
        {
            return 1m;
        }

        var cambio = RoundMoney(informado);
        if (cambio <= 0)
        {
            throw new InvalidOperationException(
                $"Informe o câmbio dos {papel} (quantos R$ valem 1 {Moedas.Symbol(moeda)}).");
        }

        return cambio;
    }

    private static decimal TakeShare(decimal proporcao, decimal total, ref decimal restante, bool last)
    {
        if (last)
        {
            var resto = restante;
            restante = 0m;
            return resto;
        }

        var share = RoundMoney(proporcao * total);
        if (share > restante)
        {
            share = restante;
        }

        if (share < 0)
        {
            share = 0m;
        }

        restante -= share;
        return share;
    }
}
