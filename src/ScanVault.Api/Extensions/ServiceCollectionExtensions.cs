using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using ScanVault.Api.Data;
using ScanVault.Api.Repositories;
using ScanVault.Api.Services;
using ScanVault.Api.Storage;

namespace ScanVault.Api.Extensions;

/// <summary>
/// Registers everything with the dependency-injection container.
/// Configuration decides which database and which file storage are used:
///   Local development: SQLite + files on disk (no cloud account needed)
///   Azure:             Azure SQL + Azure Blob Storage
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScanVault(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        // ----- Database -----
        var provider = config["Database:Provider"] ?? "Sqlite";
        var connectionString = config.GetConnectionString("ScanVault")
                               ?? throw new InvalidOperationException("Connection string 'ScanVault' is missing.");

        services.AddDbContext<ScanVaultDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
            else
                options.UseSqlite(connectionString);
        });

        // ----- File storage -----
        var storageProvider = config["Storage:Provider"] ?? "Local";
        if (storageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            var blobConnection = config["Storage:AzureBlob:ConnectionString"]
                                 ?? throw new InvalidOperationException("Storage:AzureBlob:ConnectionString is missing.");
            var containerName = config["Storage:AzureBlob:Container"] ?? "scans";
            services.AddSingleton<IFileStorage>(new AzureBlobFileStorage(new BlobContainerClient(blobConnection, containerName)));
        }
        else
        {
            var folder = config["Storage:Local:Path"] ?? "App_Data/scans";
            var fullPath = Path.IsPathRooted(folder) ? folder : Path.Combine(env.ContentRootPath, folder);
            services.AddSingleton<IFileStorage>(new LocalFileStorage(fullPath));
        }

        // ----- Repositories and services (one instance per HTTP request) -----
        services.AddScoped<ISiteRepository, SiteRepository>();
        services.AddScoped<IScanRepository, ScanRepository>();
        services.AddScoped<IAnnotationRepository, AnnotationRepository>();

        services.AddScoped<ISiteService, SiteService>();
        services.AddScoped<IScanService, ScanService>();
        services.AddScoped<IAnnotationService, AnnotationService>();

        return services;
    }

    /// <summary>
    /// Prepares the database on startup.
    /// SQL Server / Azure SQL: applies EF Core migrations (versioned schema changes).
    /// SQLite (local dev and tests): creates the tables directly from the model.
    /// </summary>
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ScanVaultDbContext>();

        if (db.Database.IsSqlServer())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
            var folder = Path.GetDirectoryName(Path.GetFullPath(dataSource));
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            await db.Database.EnsureCreatedAsync();
        }
    }
}
