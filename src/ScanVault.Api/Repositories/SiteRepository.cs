using Microsoft.EntityFrameworkCore;
using ScanVault.Api.Data;
using ScanVault.Api.Models;

namespace ScanVault.Api.Repositories;

public interface ISiteRepository : IRepository<Site>
{
    /// <summary>Sites with their scans loaded, for the list page (scan count, last capture).</summary>
    Task<List<Site>> GetAllWithScansAsync();

    /// <summary>One site with its full scan history.</summary>
    Task<Site?> GetWithScansAsync(int id);
}

public class SiteRepository : Repository<Site>, ISiteRepository
{
    public SiteRepository(ScanVaultDbContext db) : base(db) { }

    public Task<List<Site>> GetAllWithScansAsync() =>
        Set.AsNoTracking()
           .Include(s => s.Scans)
           .OrderBy(s => s.Name)
           .ToListAsync();

    public Task<Site?> GetWithScansAsync(int id) =>
        Set.Include(s => s.Scans)
           .ThenInclude(sc => sc.Annotations)
           .FirstOrDefaultAsync(s => s.Id == id);
}
