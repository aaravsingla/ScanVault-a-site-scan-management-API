using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ScanVault.Api.Data;

namespace ScanVault.Tests.Helpers;

/// <summary>
/// A real relational database (SQLite) that lives only in memory for one test.
/// Using a real database means foreign keys and cascades behave like production.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDb()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    public ScanVaultDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ScanVaultDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
