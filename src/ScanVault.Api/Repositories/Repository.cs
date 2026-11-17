using Microsoft.EntityFrameworkCore;
using ScanVault.Api.Data;
using ScanVault.Api.Models;

namespace ScanVault.Api.Repositories;

/// <summary>
/// Default EF Core implementation of <see cref="IRepository{T}"/>.
/// Specific repositories inherit from it and add their own queries.
/// </summary>
public class Repository<T> : IRepository<T> where T : EntityBase
{
    protected readonly ScanVaultDbContext Db;
    protected readonly DbSet<T> Set;

    public Repository(ScanVaultDbContext db)
    {
        Db = db;
        Set = db.Set<T>();
    }

    public virtual Task<T?> GetByIdAsync(int id) => Set.FirstOrDefaultAsync(e => e.Id == id);

    public virtual Task<List<T>> GetAllAsync() => Set.AsNoTracking().ToListAsync();

    public async Task AddAsync(T entity) => await Set.AddAsync(entity);

    public void Remove(T entity) => Set.Remove(entity);

    public Task SaveChangesAsync() => Db.SaveChangesAsync();
}
