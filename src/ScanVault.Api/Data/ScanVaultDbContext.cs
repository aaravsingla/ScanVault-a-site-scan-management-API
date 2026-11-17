using Microsoft.EntityFrameworkCore;
using ScanVault.Api.Models;

namespace ScanVault.Api.Data;

/// <summary>
/// EF Core's gateway to the database. Each DbSet maps to one table.
/// OnModelCreating configures columns, lengths and relationships (Fluent API).
/// </summary>
public class ScanVaultDbContext : DbContext
{
    public ScanVaultDbContext(DbContextOptions<ScanVaultDbContext> options) : base(options) { }

    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Scan> Scans => Set<Scan>();
    public DbSet<Annotation> Annotations => Set<Annotation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Site>(site =>
        {
            site.Property(s => s.Name).HasMaxLength(120).IsRequired();
            site.Property(s => s.Location).HasMaxLength(200);
            site.Property(s => s.Description).HasMaxLength(1000);

            // One site -> many scans. Deleting a site deletes its scans (cascade).
            site.HasMany(s => s.Scans)
                .WithOne(sc => sc.Site!)
                .HasForeignKey(sc => sc.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Scan>(scan =>
        {
            scan.Property(s => s.Title).HasMaxLength(150).IsRequired();
            scan.Property(s => s.Notes).HasMaxLength(1000);
            scan.Property(s => s.OriginalFileName).HasMaxLength(260).IsRequired();
            scan.Property(s => s.ContentType).HasMaxLength(100).IsRequired();
            scan.Property(s => s.StorageKey).HasMaxLength(300).IsRequired();
            scan.Property(s => s.Format).HasConversion<string>().HasMaxLength(20);

            // Speeds up "all scans of a site, newest first" (the history view).
            scan.HasIndex(s => new { s.SiteId, s.CapturedAt });

            // One scan -> many annotations, deleted together with the scan.
            scan.HasMany(s => s.Annotations)
                .WithOne(a => a.Scan!)
                .HasForeignKey(a => a.ScanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Annotation>(annotation =>
        {
            annotation.Property(a => a.Text).HasMaxLength(500).IsRequired();
            annotation.Property(a => a.Category).HasConversion<string>().HasMaxLength(20);
        });

        // All times are stored in UTC, but SQL Server returns them without a time zone.
        // Marking them as UTC on the way out makes the JSON end in "Z", so browsers convert correctly.
        var utcConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            toDb => toDb.ToUniversalTime(),
            fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc));

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(DateTime)))
                property.SetValueConverter(utcConverter);
    }
}
