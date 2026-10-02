using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PTRON.Models;

namespace PTRON.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>User id applied to query filters and to new rows. Empty sees nothing.</summary>
    public string CurrentUserId { get; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : this(options, currentUserId: "")
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, string currentUserId)
        : base(options)
    {
        CurrentUserId = currentUserId ?? "";
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

        // Child rows stay visible only when both parents belong to the current user.
        modelBuilder.Entity<EquipamentoInsumo>()
            .HasQueryFilter(ei => ei.Equipamento!.UserId == CurrentUserId && ei.Insumo!.UserId == CurrentUserId);
        modelBuilder.Entity<EntradaEstoqueItem>()
            .HasQueryFilter(it => it.EntradaEstoque!.UserId == CurrentUserId && it.Insumo!.UserId == CurrentUserId);
        modelBuilder.Entity<ProdutoInsumo>()
            .HasQueryFilter(pi => pi.Produto!.UserId == CurrentUserId && pi.Insumo!.UserId == CurrentUserId);

        ConfigureOwnership<TipoInsumo>(modelBuilder, uniqueName: true);
        ConfigureOwnership<Insumo>(modelBuilder, uniqueName: false);
        ConfigureOwnership<Equipamento>(modelBuilder, uniqueName: false);
        ConfigureOwnership<EntradaEstoque>(modelBuilder, uniqueName: false);
        ConfigureOwnership<Produto>(modelBuilder, uniqueName: false);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampUser();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampUser();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ConfigureOwnership<TEntity>(ModelBuilder modelBuilder, bool uniqueName)
        where TEntity : class, IUserOwned
    {
        var entity = modelBuilder.Entity<TEntity>();
        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasQueryFilter(e => e.UserId == CurrentUserId);

        if (uniqueName)
        {
            entity.HasIndex(nameof(IUserOwned.UserId), "Nome").IsUnique();
        }
        else
        {
            entity.HasIndex(e => e.UserId);
        }
    }

    /// <summary>New rows belong to the current user. UserId never changes afterwards.</summary>
    private void StampUser()
    {
        foreach (var entry in ChangeTracker.Entries<IUserOwned>())
        {
            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrEmpty(CurrentUserId))
                {
                    throw new InvalidOperationException(
                        "Não há usuário autenticado para gravar estes dados.");
                }

                entry.Entity.UserId = CurrentUserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(e => e.UserId).IsModified = false;
            }
        }
    }
}
