using ScanVault.Api.Dtos;
using ScanVault.Api.Errors;
using ScanVault.Api.Models;
using ScanVault.Api.Repositories;
using ScanVault.Api.Storage;

namespace ScanVault.Api.Services;

public interface ISiteService
{
    Task<List<SiteSummaryDto>> GetAllAsync();
    Task<SiteDetailDto> GetAsync(int id);
    Task<SiteDetailDto> CreateAsync(CreateSiteRequest request);
    Task<SiteDetailDto> UpdateAsync(int id, UpdateSiteRequest request);
    Task DeleteAsync(int id);
}

/// <summary>Business logic for sites. Controllers call this; this calls repositories.</summary>
public class SiteService : ISiteService
{
    private readonly ISiteRepository _sites;
    private readonly IFileStorage _storage;

    public SiteService(ISiteRepository sites, IFileStorage storage)
    {
        _sites = sites;
        _storage = storage;
    }

    public async Task<List<SiteSummaryDto>> GetAllAsync()
    {
        var sites = await _sites.GetAllWithScansAsync();
        return sites.Select(s => s.ToSummaryDto()).ToList();
    }

    public async Task<SiteDetailDto> GetAsync(int id)
    {
        var site = await FindAsync(id);
        return site.ToDetailDto();
    }

    public async Task<SiteDetailDto> CreateAsync(CreateSiteRequest request)
    {
        var site = new Site
        {
            Name = request.Name.Trim(),
            Location = request.Location?.Trim(),
            Description = request.Description?.Trim()
        };

        await _sites.AddAsync(site);
        await _sites.SaveChangesAsync();
        return site.ToDetailDto();
    }

    public async Task<SiteDetailDto> UpdateAsync(int id, UpdateSiteRequest request)
    {
        var site = await FindAsync(id);
        site.Name = request.Name.Trim();
        site.Location = request.Location?.Trim();
        site.Description = request.Description?.Trim();

        await _sites.SaveChangesAsync();
        return site.ToDetailDto();
    }

    public async Task DeleteAsync(int id)
    {
        var site = await FindAsync(id);
        var fileKeys = site.Scans.Select(s => s.StorageKey).ToList();

        // The database cascade removes scans and annotations; then we clean up their files.
        _sites.Remove(site);
        await _sites.SaveChangesAsync();

        foreach (var key in fileKeys)
            await _storage.DeleteAsync(key);
    }

    private async Task<Site> FindAsync(int id) =>
        await _sites.GetWithScansAsync(id)
        ?? throw new NotFoundException($"Site {id} was not found.");
}
