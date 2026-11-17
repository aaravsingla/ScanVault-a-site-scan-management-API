using Microsoft.EntityFrameworkCore;
using ScanVault.Api.Data;
using ScanVault.Api.Models;

namespace ScanVault.Api.Repositories;

public interface IScanRepository : IRepository<Scan>
{
    /// <summary>Scan history of one site, newest capture first.</summary>
    Task<List<Scan>> GetBySiteAsync(int siteId);

    Task<Scan?> GetWithAnnotationsAsync(int id);
}

public class ScanRepository : Repository<Scan>, IScanRepository
{
    public ScanRepository(ScanVaultDbContext db) : base(db) { }

    public Task<List<Scan>> GetBySiteAsync(int siteId) =>
        Set.AsNoTracking()
           .Include(s => s.Annotations)
           .Where(s => s.SiteId == siteId)
           .OrderByDescending(s => s.CapturedAt)
           .ToListAsync();

    public Task<Scan?> GetWithAnnotationsAsync(int id) =>
        Set.Include(s => s.Annotations)
           .FirstOrDefaultAsync(s => s.Id == id);
}
