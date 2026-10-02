using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using PTRON.Data;
using PTRON.Identity;
using PTRON.Models;
using PTRON.Services;
using Xunit;

namespace PTRON.Tests;

public sealed class UserOwnershipTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<AppDbContext> _options = null!;
    private ServiceProvider _root = null!;
    private string _anaId = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddPtronIdentity(configuration);
        _root = services.BuildServiceProvider();

        await using (var db = new AppDbContext(_options, ReservedUsers.LeoId))
        {
            await db.Database.MigrateAsync();
        }

        using var scope = _root.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var ana = new ApplicationUser
        {
            UserName = "ana@example.com",
            Email = "ana@example.com",
            EmailConfirmed = true
        };
        var created = await users.CreateAsync(ana, "Senha123");
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        _anaId = ana.Id;
    }

    public async Task DisposeAsync()
    {
        await _root.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task New_rows_are_stamped_with_the_current_user_and_hidden_from_others()
    {
        int tipoId;
        await using (var ana = new AppDbContext(_options, _anaId))
        {
            ana.TiposInsumo.Add(new TipoInsumo { Nome = "Bobina", UserId = ReservedUsers.LeoId });
            await ana.SaveChangesAsync();
            tipoId = ana.TiposInsumo.Single().Id;
            Assert.Equal(_anaId, ana.TiposInsumo.Single().UserId);
        }

        await using var leo = new AppDbContext(_options, ReservedUsers.LeoId);
        Assert.Null(await leo.TiposInsumo.FirstOrDefaultAsync(t => t.Id == tipoId));
    }

    [Fact]
    public async Task The_same_type_name_can_exist_for_two_users()
    {
        await using (var leo = new AppDbContext(_options, ReservedUsers.LeoId))
        {
            leo.TiposInsumo.Add(new TipoInsumo { Nome = "Resistor" });
            await leo.SaveChangesAsync();
        }

        await using var ana = new AppDbContext(_options, _anaId);
        ana.TiposInsumo.Add(new TipoInsumo { Nome = "Resistor" });
        await ana.SaveChangesAsync();

        Assert.Equal("Resistor", ana.TiposInsumo.Single().Nome);
    }

    [Fact]
    public async Task Lookup_and_update_of_another_users_type_report_not_found()
    {
        var leoTipos = new TipoInsumoService(new UserFactory(_options, ReservedUsers.LeoId));
        var anaTipos = new TipoInsumoService(new UserFactory(_options, _anaId));

        await leoTipos.CreateAsync(new TipoInsumo { Nome = "Capacitor" });
        var leoTipo = (await leoTipos.GetAllAsync()).Single();

        Assert.Null(await anaTipos.GetAsync(leoTipo.Id));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => anaTipos.UpdateAsync(new TipoInsumo { Id = leoTipo.Id, Nome = "Outro" }));
        Assert.Equal("Tipo não encontrado.", error.Message);
    }

    [Fact]
    public async Task Insumo_cannot_reference_another_users_type()
    {
        var leoTipos = new TipoInsumoService(new UserFactory(_options, ReservedUsers.LeoId));
        await leoTipos.CreateAsync(new TipoInsumo { Nome = "Diodo" });
        var tipoId = (await leoTipos.GetAllAsync()).Single().Id;

        var anaInsumos = new InsumoService(new UserFactory(_options, _anaId), new ImageUploadService(new TempHost()));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => anaInsumos.CreateAsync(new Insumo { Nome = "1N4007", TipoInsumoId = tipoId }));
        Assert.Equal("Tipo não encontrado.", error.Message);
    }

    [Fact]
    public async Task Stock_entry_and_bom_cannot_use_another_users_insumo()
    {
        int insumoId;
        int equipamentoId;
        await using (var leo = new AppDbContext(_options, ReservedUsers.LeoId))
        {
            var tipo = new TipoInsumo { Nome = "CI" };
            leo.TiposInsumo.Add(tipo);
            await leo.SaveChangesAsync();

            var insumo = new Insumo { Nome = "LM358", TipoInsumoId = tipo.Id };
            leo.Insumos.Add(insumo);
            var equipamento = new Equipamento { Nome = "Fonte" };
            leo.Equipamentos.Add(equipamento);
            await leo.SaveChangesAsync();

            leo.EquipamentoInsumos.Add(new EquipamentoInsumo
            {
                EquipamentoId = equipamento.Id,
                InsumoId = insumo.Id,
                Qtd = 1
            });
            await leo.SaveChangesAsync();
            insumoId = insumo.Id;
            equipamentoId = equipamento.Id;
        }

        var anaEntradas = new EntradaEstoqueService(new UserFactory(_options, _anaId));
        var entradaError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            anaEntradas.FinalizarAsync(new EntradaCommand
            {
                Moeda = Moedas.Brl,
                Cambio = 1,
                Itens = new[] { new EntradaLinhaInput { InsumoId = insumoId, Qtd = 1, PrecoUnitario = 2 } }
            }));
        Assert.Contains("não encontrado", entradaError.Message, StringComparison.Ordinal);

        var anaEquipamentos = new EquipamentoService(
            new UserFactory(_options, _anaId), new ImageUploadService(new TempHost()));
        var bomError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => anaEquipamentos.AddInsumoAsync(equipamentoId, insumoId, 1));
        Assert.Equal("Equipamento não encontrado.", bomError.Message);

        await anaEquipamentos.RemoveInsumoAsync(equipamentoId, insumoId);

        await using var leoCheck = new AppDbContext(_options, ReservedUsers.LeoId);
        Assert.True(await leoCheck.EquipamentoInsumos.AnyAsync(ei => ei.EquipamentoId == equipamentoId));
        Assert.Equal(0, (await leoCheck.Insumos.SingleAsync(i => i.Id == insumoId)).Saldo);
    }

    [Fact]
    public async Task Saving_without_a_user_is_rejected()
    {
        await using var db = new AppDbContext(_options, "");
        db.TiposInsumo.Add(new TipoInsumo { Nome = "Válvula" });
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Equal("Não há usuário autenticado para gravar estes dados.", error.Message);
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

    private sealed class TempHost : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "PTRON.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
