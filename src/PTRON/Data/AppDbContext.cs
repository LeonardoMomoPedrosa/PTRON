using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PTRON.Models;

namespace PTRON.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TipoInsumo> TiposInsumo => Set<TipoInsumo>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<Equipamento> Equipamentos => Set<Equipamento>();
    public DbSet<EquipamentoInsumo> EquipamentoInsumos => Set<EquipamentoInsumo>();
    public DbSet<EntradaEstoque> EntradasEstoque => Set<EntradaEstoque>();
    public DbSet<EntradaEstoqueItem> EntradaEstoqueItens => Set<EntradaEstoqueItem>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<ProdutoInsumo> ProdutoInsumos => Set<ProdutoInsumo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(user =>
        {
            user.Property(u => u.Pais).HasMaxLength(2);
            user.Property(u => u.Estado).HasMaxLength(100);
            user.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
        });

        // SQLite stores decimals as TEXT with fixed precision for correct math/sorting.
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(4);
        }

        modelBuilder.Entity<Insumo>()
            .HasOne(i => i.TipoInsumo)
            .WithMany(t => t.Insumos)
            .HasForeignKey(i => i.TipoInsumoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EquipamentoInsumo>()
            .HasOne(ei => ei.Equipamento)
            .WithMany(e => e.Insumos)
            .HasForeignKey(ei => ei.EquipamentoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EquipamentoInsumo>()
            .HasOne(ei => ei.Insumo)
            .WithMany(i => i.EquipamentoInsumos)
            .HasForeignKey(ei => ei.InsumoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EntradaEstoqueItem>()
            .HasOne(it => it.EntradaEstoque)
            .WithMany(e => e.Itens)
            .HasForeignKey(it => it.EntradaEstoqueId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EntradaEstoqueItem>()
            .HasOne(it => it.Insumo)
            .WithMany()
            .HasForeignKey(it => it.InsumoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Produto>()
            .HasOne(p => p.Equipamento)
            .WithMany()
            .HasForeignKey(p => p.EquipamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProdutoInsumo>()
            .HasOne(pi => pi.Produto)
            .WithMany(p => p.Insumos)
            .HasForeignKey(pi => pi.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProdutoInsumo>()
            .HasOne(pi => pi.Insumo)
            .WithMany()
            .HasForeignKey(pi => pi.InsumoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
