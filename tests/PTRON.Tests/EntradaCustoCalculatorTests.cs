using PTRON.Api;
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
            MoedaProdutos = Moedas.Brl,
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
        Assert.Equal(Moedas.Brl, rateio.MoedaFrete);

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
            MoedaProdutos = Moedas.Usd,
            CambioProdutos = 5.5m,
            MoedaImpostos = Moedas.Usd,
            MoedaFrete = Moedas.Usd,
            Frete = 10m,
            Impostos = 5m,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 2m, PrecoUnitario = 10m },
                new EntradaLinhaInput { InsumoId = 2, Qtd = 1m, PrecoUnitario = 30m }
            }
        });

        Assert.Equal(Moedas.Usd, rateio.MoedaProdutos);
        Assert.Equal(5.5m, rateio.CambioProdutos);
        Assert.Equal(5.5m, rateio.CambioImpostos);
        Assert.Equal(5.5m, rateio.CambioFrete);
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
            MoedaProdutos = Moedas.Brl,
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
            MoedaProdutos = Moedas.Brl,
            CambioProdutos = 9m,
            CambioImpostos = 9m,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 1m, PrecoUnitario = 8m }
            }
        });

        Assert.Equal(1m, rateio.CambioProdutos);
        Assert.Equal(1m, rateio.CambioImpostos);
        Assert.Equal(8m, rateio.Linhas[0].CustoUnitarioBrl);
    }

    [Fact]
    public void Dolar_sem_cambio_falha()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => EntradaCustoCalculator.Calcular(new EntradaCommand
        {
            MoedaProdutos = Moedas.Usd,
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
            MoedaProdutos = Moedas.Brl,
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

    [Fact]
    public void Produtos_em_dolar_impostos_em_real_e_frete_na_moeda_dos_produtos()
    {
        var rateio = EntradaCustoCalculator.Calcular(new EntradaCommand
        {
            MoedaProdutos = Moedas.Usd,
            CambioProdutos = 5.5m,
            MoedaImpostos = Moedas.Brl,
            MoedaFrete = Moedas.Usd,
            Frete = 10m,
            Impostos = 5m,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 2m, PrecoUnitario = 10m },
                new EntradaLinhaInput { InsumoId = 2, Qtd = 1m, PrecoUnitario = 30m }
            }
        });

        Assert.Equal(4m, rateio.Linhas[0].FreteRateado);
        Assert.Equal(2m, rateio.Linhas[0].ImpostoRateado);
        Assert.Equal(134m, rateio.Linhas[0].SubtotalBrl);
        Assert.Equal(67m, rateio.Linhas[0].CustoUnitarioBrl);
        Assert.Equal(201m, rateio.Linhas[1].SubtotalBrl);
        Assert.Equal(335m, rateio.TotalBrl);
        Assert.Equal(1m, rateio.CambioImpostos);
        Assert.Equal(5.5m, rateio.CambioFrete);
    }

    [Fact]
    public void Frete_em_reais_nao_usa_o_cambio_dos_produtos()
    {
        var rateio = EntradaCustoCalculator.Calcular(new EntradaCommand
        {
            MoedaProdutos = Moedas.Usd,
            CambioProdutos = 5.5m,
            MoedaImpostos = Moedas.Brl,
            MoedaFrete = Moedas.Brl,
            Frete = 10m,
            Impostos = 5m,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 2m, PrecoUnitario = 10m },
                new EntradaLinhaInput { InsumoId = 2, Qtd = 1m, PrecoUnitario = 30m }
            }
        });

        Assert.Equal(116m, rateio.Linhas[0].SubtotalBrl);
        Assert.Equal(58m, rateio.Linhas[0].CustoUnitarioBrl);
        Assert.Equal(174m, rateio.Linhas[1].SubtotalBrl);
        Assert.Equal(290m, rateio.TotalBrl);
        Assert.Equal(1m, rateio.CambioFrete);
    }

    [Fact]
    public void Euro_e_dolar_usam_cambios_independentes()
    {
        var rateio = EntradaCustoCalculator.Calcular(new EntradaCommand
        {
            MoedaProdutos = Moedas.Eur,
            CambioProdutos = 6m,
            MoedaImpostos = Moedas.Usd,
            CambioImpostos = 5m,
            MoedaFrete = Moedas.Usd,
            Frete = 2m,
            Impostos = 3m,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 1m, PrecoUnitario = 10m }
            }
        });

        Assert.Equal(85m, rateio.TotalBrl);
        Assert.Equal(85m, rateio.Linhas[0].CustoUnitarioBrl);
        Assert.Equal(5m, rateio.CambioFrete);
    }

    [Fact]
    public void Impostos_na_mesma_moeda_dos_produtos_reusam_o_cambio()
    {
        var rateio = EntradaCustoCalculator.Calcular(new EntradaCommand
        {
            MoedaProdutos = Moedas.Usd,
            CambioProdutos = 5m,
            MoedaImpostos = Moedas.Usd,
            CambioImpostos = 9m,
            Frete = 0m,
            Impostos = 2m,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 1m, PrecoUnitario = 10m }
            }
        });

        Assert.Equal(5m, rateio.CambioImpostos);
        Assert.Equal(60m, rateio.TotalBrl);
    }

    [Fact]
    public void Frete_em_moeda_que_nao_e_nenhuma_das_tres_falha()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => EntradaCustoCalculator.Calcular(new EntradaCommand
        {
            MoedaProdutos = Moedas.Usd,
            CambioProdutos = 5m,
            MoedaImpostos = Moedas.Brl,
            MoedaFrete = Moedas.Eur,
            Itens = new[]
            {
                new EntradaLinhaInput { InsumoId = 1, Qtd = 1m, PrecoUnitario = 1m }
            }
        }));

        Assert.Contains("frete", ex.Message);
    }

    [Fact]
    public void Payload_antigo_de_uma_moeda_continua_valendo()
    {
        var command = new EntradaEstoqueWriteDto
        {
            Moeda = Moedas.Usd,
            Cambio = 5.5m,
            Frete = 1m,
            Impostos = 2m,
            Itens = new List<EntradaEstoqueItemWriteDto>
            {
                new() { InsumoId = 1, Qtd = 1m, PrecoUnitario = 10m }
            }
        }.ToCommand();

        var rateio = EntradaCustoCalculator.Calcular(command);

        Assert.Equal(Moedas.Usd, rateio.MoedaProdutos);
        Assert.Equal(Moedas.Usd, rateio.MoedaImpostos);
        Assert.Equal(Moedas.Usd, rateio.MoedaFrete);
        Assert.Equal(5.5m, rateio.CambioProdutos);
        Assert.Equal(71.5m, rateio.TotalBrl);
    }
}
