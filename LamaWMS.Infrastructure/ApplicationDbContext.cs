using LamaWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace LamaWMS.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Fabric> Fabrics => Set<Fabric>();
    public DbSet<Roll> Rolls => Set<Roll>();
    public DbSet<Cell> Cells => Set<Cell>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductFabric> ProductFabrics => Set<ProductFabric>();
    public DbSet<RollTransaction> RollTransactions => Set<RollTransaction>();
    public DbSet<RollReservation> RollReservations => Set<RollReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureFabric(modelBuilder);
        ConfigureRoll(modelBuilder);
        ConfigureCell(modelBuilder);
        ConfigureOrder(modelBuilder);
        ConfigureProduct(modelBuilder);
        ConfigureProductFabric(modelBuilder);
        ConfigureRollTransaction(modelBuilder);
        ConfigureRollReservation(modelBuilder);
    }

    private static void ConfigureFabric(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Fabric>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Color)
                .HasMaxLength(100)
                .IsRequired();
        });
    }

    private static void ConfigureRoll(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Roll>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Number)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.Number)
                .IsUnique();

            entity.Property(x => x.InitialLength)
                .HasPrecision(10, 2);

            entity.Property(x => x.CurrentLength)
                .HasPrecision(10, 2);

            entity.HasOne(x => x.Fabric)
                .WithMany(x => x.Rolls)
                .HasForeignKey(x => x.FabricId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Cell)
                .WithMany(x => x.Rolls)
                .HasForeignKey(x => x.CellId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureCell(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cell>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.Code)
                .IsUnique();
        });
    }

    private static void ConfigureOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.CustomerName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Number)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.Number)
                .IsUnique();
        });
    }

    private static void ConfigureProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasOne(x => x.Order)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProductFabric(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductFabric>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.RequiredLength)
                .HasPrecision(10, 2);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.ProductFabrics)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Fabric)
                .WithMany(x => x.ProductFabrics)
                .HasForeignKey(x => x.FabricId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.ProductId,
                x.FabricId
            }).IsUnique();
        });
    }

    private static void ConfigureRollTransaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RollTransaction>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Length)
                .HasPrecision(10, 2);

            entity.Property(x => x.Comment)
                .HasMaxLength(500);

            entity.HasOne(x => x.Roll)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.RollId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.RollTransactions)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureRollReservation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RollReservation>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ReservedLength)
                .HasPrecision(10, 2);

            entity.HasOne(x => x.Roll)
                .WithMany(x => x.Reservations)
                .HasForeignKey(x => x.RollId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ProductFabric)
                .WithMany(x => x.RollReservations)
                .HasForeignKey(x => x.ProductFabricId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}