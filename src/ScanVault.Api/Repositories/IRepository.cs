using ScanVault.Api.Models;

namespace ScanVault.Api.Repositories;

/// <summary>
/// Generic data-access contract shared by all entities (generics + interfaces).
/// Services depend on this interface, not on EF Core directly, so they are easy to test.
/// </summary>
public interface IRepository<T> where T : EntityBase
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Remove(T entity);
    Task SaveChangesAsync();
}
