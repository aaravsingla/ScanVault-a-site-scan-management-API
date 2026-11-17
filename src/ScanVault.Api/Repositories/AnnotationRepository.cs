using Microsoft.EntityFrameworkCore;
using ScanVault.Api.Data;
using ScanVault.Api.Models;

namespace ScanVault.Api.Repositories;

public interface IAnnotationRepository : IRepository<Annotation>
{
    Task<List<Annotation>> GetByScanAsync(int scanId);
}

public class AnnotationRepository : Repository<Annotation>, IAnnotationRepository
{
    public AnnotationRepository(ScanVaultDbContext db) : base(db) { }

    public Task<List<Annotation>> GetByScanAsync(int scanId) =>
        Set.AsNoTracking()
           .Where(a => a.ScanId == scanId)
           .OrderBy(a => a.CreatedAt)
           .ToListAsync();
}
