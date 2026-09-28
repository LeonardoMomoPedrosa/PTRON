using PTRON.Services;
using Xunit;

namespace PTRON.Tests;

public class EntradaCustoCalculatorTests
{
    [Fact]
    public void Rateia_frete_e_impostos_pela_proporcao_dos_produtos()
    {
        var rateio = EntradaCustoCalculator.Calcular(new EntradaCommand
        {
            Moeda = Moedas.Brl,
            Frete = 10m,
            Impostos = 5m,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 2m, PrecoUnitario = 10m },
                new EntradaLinhaInput { InsumoId = 2, Qtd = 1m, PrecoUnitario = 30m }
            }
        });

        Assert.Equal(50m, rateio.TotalProdutos);
        Assert.Equal(65m, rateio.TotalMoeda);
        Assert.Equal(65m, rateio.TotalBrl);

        var a = rateio.Linhas[0];
        Assert.Equal(0.4m, a.Proporcao);
        Assert.Equal(4m, a.FreteRateado);
        Assert.Equal(2m, a.ImpostoRateado);
        Assert.Equal(13m, a.CustoUnitarioBrl);
        Assert.Equal(26m, a.SubtotalBrl);

        var b = rateio.Linhas[1];
        Assert.Equal(0.6m, b.Proporcao);
        Assert.Equal(6m, b.FreteRateado);
        Assert.Equal(3m, b.ImpostoRateado);
        Assert.Equal(39m, b.CustoUnitarioBrl);
        Assert.Equal(39m, b.SubtotalBrl);
    }

    [Fact]
    public void Converte_dolar_pelo_cambio_depois_do_rateio()
    {
        var rateio = EntradaCustoCalculator.Calcular(new EntradaCommand
        {
            Moeda = Moedas.Usd,
            Cambio = 5.5m,
            Frete = 10m,
            Impostos = 5m,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 2m, PrecoUnitario = 10m },
                new EntradaLinhaInput { InsumoId = 2, Qtd = 1m, PrecoUnitario = 30m }
            }
        });

        Assert.Equal(Moedas.Usd, rateio.Moeda);
        Assert.Equal(5.5m, rateio.Cambio);
        Assert.Equal(71.5m, rateio.Linhas[0].CustoUnitarioBrl);
        Assert.Equal(143m, rateio.Linhas[0].SubtotalBrl);
        Assert.Equal(214.5m, rateio.Linhas[1].CustoUnitarioBrl);
        Assert.Equal(357.5m, rateio.TotalBrl);
    }

    [Fact]
    public void Ultima_linha_absorve_o_centavo_do_arredondamento()
    {
        var rateio = EntradaCustoCalculator.Calcular(new EntradaCommand
        {
            Moeda = Moedas.Brl,
            Frete = 0.01m,
            Impostos = 0.01m,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 1m, PrecoUnitario = 10m },
                new EntradaLinhaInput { InsumoId = 2, Qtd = 1m, PrecoUnitario = 10m },
                new EntradaLinhaInput { InsumoId = 3, Qtd = 1m, PrecoUnitario = 10m }
            }
        });

        Assert.Equal(0.0033m, rateio.Linhas[0].FreteRateado);
        Assert.Equal(0.0033m, rateio.Linhas[1].FreteRateado);
        Assert.Equal(0.0034m, rateio.Linhas[2].FreteRateado);
        Assert.Equal(0.01m, rateio.Linhas.Sum(l => l.FreteRateado));
        Assert.Equal(0.01m, rateio.Linhas.Sum(l => l.ImpostoRateado));
    }

    [Fact]
    public void Real_ignora_cambio_informado()
    {
        var rateio = EntradaCustoCalculator.Calcular(new EntradaCommand
        {
            Moeda = Moedas.Brl,
            Cambio = 9m,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 1m, PrecoUnitario = 8m }
            }
        });

        Assert.Equal(1m, rateio.Cambio);
        Assert.Equal(8m, rateio.Linhas[0].CustoUnitarioBrl);
    }

    [Fact]
    public void Dolar_sem_cambio_falha()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => EntradaCustoCalculator.Calcular(new EntradaCommand
        {
            Moeda = Moedas.Usd,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 1m, PrecoUnitario = 1m }
            }
        }));

        Assert.Contains("câmbio", ex.Message);
    }

    [Fact]
    public void Precos_zerados_rateiam_pela_quantidade()
    {
        var rateio = EntradaCustoCalculator.Calcular(new EntradaCommand
        {
            Moeda = Moedas.Brl,
            Frete = 9m,
            Impostos = 0m,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 1m, PrecoUnitario = 0m },
                new EntradaLinhaInput { InsumoId = 2, Qtd = 2m, PrecoUnitario = 0m }
            }
        });

        Assert.Equal(3m, rateio.Linhas[0].FreteRateado);
        Assert.Equal(6m, rateio.Linhas[1].FreteRateado);
        Assert.Equal(3m, rateio.Linhas[0].CustoUnitarioBrl);
        Assert.Equal(3m, rateio.Linhas[1].CustoUnitarioBrl);
    }
}
