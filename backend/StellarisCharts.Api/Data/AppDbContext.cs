using Microsoft.EntityFrameworkCore;
using StellarisCharts.Api.Models;

namespace StellarisCharts.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Snapshot> Snapshots => Set<Snapshot>();
    public DbSet<BudgetLineItem> BudgetLineItems => Set<BudgetLineItem>();
    public DbSet<SpeciesPopulation> SpeciesPopulations => Set<SpeciesPopulation>();
    public DbSet<GlobalSpeciesPopulation> GlobalSpeciesPopulations => Set<GlobalSpeciesPopulation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Country configuration
        modelBuilder.Entity<Country>()
            .HasIndex(c => c.CountryId)
            .IsUnique();

        modelBuilder.Entity<Country>()
            .HasMany(c => c.Snapshots)
            .WithOne(s => s.Country)
            .HasForeignKey(s => s.CountryId)
            .HasPrincipalKey(c => c.CountryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Snapshot configuration
        modelBuilder.Entity<Snapshot>()
            .HasMany(s => s.BudgetLineItems)
            .WithOne(b => b.Snapshot)
            .HasForeignKey(b => b.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        // BudgetLineItem configuration
        modelBuilder.Entity<BudgetLineItem>()
            .HasOne(b => b.Country)
            .WithMany()
            .HasForeignKey(b => b.CountryId)
            .HasPrincipalKey(c => c.CountryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BudgetLineItem>()
            .HasIndex(li => new { li.SnapshotId, li.CountryId, li.Section });

        modelBuilder.Entity<BudgetLineItem>()
            .Property(li => li.Section)
            .HasMaxLength(16);

        modelBuilder.Entity<BudgetLineItem>()
            .Property(li => li.Category)
            .HasMaxLength(128);

        modelBuilder.Entity<BudgetLineItem>()
            .Property(li => li.ResourceType)
            .HasMaxLength(64);

        modelBuilder.Entity<SpeciesPopulation>()
            .HasOne(sp => sp.Snapshot)
            .WithMany()
            .HasForeignKey(sp => sp.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SpeciesPopulation>()
            .HasOne(sp => sp.Country)
            .WithMany()
            .HasForeignKey(sp => sp.CountryId)
            .HasPrincipalKey(c => c.CountryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SpeciesPopulation>()
            .HasIndex(sp => new { sp.SnapshotId, sp.CountryId, sp.SpeciesId });

        modelBuilder.Entity<SpeciesPopulation>()
            .Property(sp => sp.SpeciesName)
            .HasMaxLength(128);

        modelBuilder.Entity<GlobalSpeciesPopulation>()
            .HasOne(sp => sp.Snapshot)
            .WithMany()
            .HasForeignKey(sp => sp.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GlobalSpeciesPopulation>()
            .HasIndex(sp => sp.SnapshotId);

        modelBuilder.Entity<GlobalSpeciesPopulation>()
            .HasIndex(sp => new { sp.SnapshotId, sp.SpeciesId });

        modelBuilder.Entity<GlobalSpeciesPopulation>()
            .Property(sp => sp.SpeciesName)
            .HasMaxLength(128);
    }
}
