using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PTRON.Data;
using PTRON.Models;
using PTRON.Services;
using Xunit;

namespace PTRON.Tests;

public sealed class ProducaoQuantidadeTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<AppDbContext> _options = null!;
    private int _equipamentoId;
    private int _insumoId;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        await using var db = new AppDbContext(_options, "leo");
        await db.Database.MigrateAsync();

        var tipo = new TipoInsumo { Nome = "Resistor" };
        db.TiposInsumo.Add(tipo);
        var insumo = new Insumo { TipoInsumo = tipo, Nome = "10k", Saldo = 6m, CustoUnitario = 2m };
        db.Insumos.Add(insumo);
        var equipamento = new Equipamento
        {
            Nome = "Fonte",
            Insumos = new List<EquipamentoInsumo> { new() { Insumo = insumo, Qtd = 3m } }
        };
        db.Equipamentos.Add(equipamento);
        await db.SaveChangesAsync();
        _equipamentoId = equipamento.Id;
        _insumoId = insumo.Id;
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task Preview_multiplica_o_consumo_pela_quantidade()
    {
        var service = new ProducaoService(new UserFactory(_options, "leo"));

        var uma = await service.GetPreviewAsync(_equipamentoId);
        Assert.NotNull(uma);
        Assert.Equal(1, uma.Quantidade);
        Assert.Equal(3m, uma.Linhas[0].QtdPorUnidade);
        Assert.Equal(3m, uma.Linhas[0].QtdNecessaria);
        Assert.Equal(6m, uma.CustoEstimado);
        Assert.True(uma.PodeProduzir);

        var duas = await service.GetPreviewAsync(_equipamentoId, 2);
        Assert.NotNull(duas);
        Assert.Equal(6m, duas.Linhas[0].QtdNecessaria);
        Assert.Equal(6m, duas.CustoPorUnidade);
        Assert.Equal(12m, duas.CustoEstimado);
        Assert.True(duas.PodeProduzir);

        var tres = await service.GetPreviewAsync(_equipamentoId, 3);
        Assert.NotNull(tres);
        Assert.Equal(9m, tres.Linhas[0].QtdNecessaria);
        Assert.False(tres.PodeProduzir);
        Assert.Equal(3m, tres.Faltantes[0].Faltante);
    }

    [Fact]
    public async Task Produzir_duas_unidades_consome_o_dobro_e_cria_dois_produtos()
    {
        var service = new ProducaoService(new UserFactory(_options, "leo"));
        var produtos = new ProdutoService(new UserFactory(_options, "leo"));

        var resultado = await service.ProduzirAsync(_equipamentoId, "lote", 2);

        Assert.Equal(2, resultado.Quantidade);
        Assert.Equal(6m, resultado.CustoUnitario);
        Assert.Equal(2, resultado.Produtos.Count);
        Assert.All(resultado.Produtos, p => Assert.Equal(6m, p.CustoTotal));
        Assert.All(resultado.Produtos, p => Assert.Equal("lote", p.DescricaoAdicional));

        await using var db = new AppDbContext(_options, "leo");
        var insumo = await db.Insumos.SingleAsync(i => i.Id == _insumoId);
        Assert.Equal(0m, insumo.Saldo);

        var gravados = await db.Produtos.Include(p => p.Insumos).OrderBy(p => p.Id).ToListAsync();
        Assert.Equal(2, gravados.Count);
        Assert.All(gravados, p =>
        {
            var linha = Assert.Single(p.Insumos);
            Assert.Equal(3m, linha.Qtd);
            Assert.Equal(2m, linha.PrecoUnitario);
        });

        await produtos.DeleteAsync(gravados[0].Id);
        await using var depois = new AppDbContext(_options, "leo");
        Assert.Equal(3m, (await depois.Insumos.SingleAsync(i => i.Id == _insumoId)).Saldo);
    }

    [Fact]
    public async Task Produzir_sem_estoque_para_a_quantidade_nao_altera_o_saldo()
    {
        var service = new ProducaoService(new UserFactory(_options, "leo"));

        var erro = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ProduzirAsync(_equipamentoId, null, 3));
        Assert.Contains("3 unidades", erro.Message);
        Assert.Contains("insuficientes", erro.Message);

        await using var db = new AppDbContext(_options, "leo");
        Assert.Equal(6m, (await db.Insumos.SingleAsync(i => i.Id == _insumoId)).Saldo);
        Assert.Empty(await db.Produtos.ToListAsync());
    }

    [Fact]
    public async Task Quantidade_menor_que_um_falha()
    {
        var service = new ProducaoService(new UserFactory(_options, "leo"));
        var erro = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ProduzirAsync(_equipamentoId, null, 0));
        Assert.Contains("pelo menos 1", erro.Message);
    }

    private sealed class UserFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly string _userId;

        public UserFactory(DbContextOptions<AppDbContext> options, string userId)
        {
            _options = options;
            _userId = userId;
        }

        public AppDbContext CreateDbContext() => new(_options, _userId);
    }
}
