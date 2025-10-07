using Microsoft.EntityFrameworkCore;
using Engitrack.Inventory.Domain.Materials;
using Engitrack.Inventory.Domain.Suppliers;
using Engitrack.Inventory.Domain.Transactions;

namespace Engitrack.Inventory.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");

        // Material Configuration
        modelBuilder.Entity<Material>(entity =>
        {
            entity.ToTable("Materials", "inventory");
            entity.HasKey(e => e.MaterialId);
            entity.Property(e => e.MaterialId).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(160).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Stock).HasPrecision(18, 3).HasDefaultValue(0);
            entity.Property(e => e.MinNivel).HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

            // Unique constraint: Name per Project
            entity.HasIndex(e => new { e.ProjectId, e.Name }).IsUnique()
                .HasDatabaseName("IX_Materials_ProjectId_Name_Unique");

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.ProjectId, e.Status });

            // Ignore domain events
            entity.Ignore(e => e.DomainEvents);
        });

        // Supplier Configuration
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers", "inventory");
            entity.HasKey(e => e.SupplierId);
            entity.Property(e => e.SupplierId).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(160).IsRequired();
            entity.Property(e => e.Ruc).HasMaxLength(20);
            entity.Property(e => e.Phone).HasMaxLength(32);
            entity.Property(e => e.Email).HasMaxLength(160);

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Ruc).IsUnique().HasFilter("[Ruc] IS NOT NULL");

            // Ignore domain events
            entity.Ignore(e => e.DomainEvents);
        });

        // InventoryTransaction Configuration
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("InventoryTransactions", "inventory");
            entity.HasKey(e => e.TxId);
            entity.Property(e => e.TxId).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.MaterialId).IsRequired();
            entity.Property(e => e.TxType).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(e => e.Quantity).HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.SupplierId);
            entity.Property(e => e.Notes).HasMaxLength(400);
            entity.Property(e => e.TxDate).HasColumnType("datetime2(3)").IsRequired();

            entity.HasIndex(e => e.MaterialId);
            entity.HasIndex(e => new { e.MaterialId, e.TxDate })
                .HasDatabaseName("IX_InventoryTransactions_MaterialId_TxDate");
            entity.HasIndex(e => e.TxDate);

            // Foreign Keys
            entity.HasOne<Material>()
                .WithMany()
                .HasForeignKey(e => e.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Supplier>()
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ignore domain events
            entity.Ignore(e => e.DomainEvents);
        });

        base.OnModelCreating(modelBuilder);
    }
}