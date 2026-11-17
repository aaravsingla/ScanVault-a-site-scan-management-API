using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ScanVault.Api.Data;

/// <summary>
/// Used only by the `dotnet ef` command-line tool when generating migrations.
/// Migrations are generated for SQL Server because production runs on Azure SQL.
/// The connection string here is never used to connect; it only selects the SQL dialect.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ScanVaultDbContext>
{
    public ScanVaultDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ScanVaultDbContext>()
            .UseSqlServer("Server=localhost;Database=ScanVault;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new ScanVaultDbContext(options);
    }
}
