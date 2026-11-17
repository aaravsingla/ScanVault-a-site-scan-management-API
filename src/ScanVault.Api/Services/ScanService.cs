using ScanVault.Api.Dtos;
using ScanVault.Api.Errors;
using ScanVault.Api.Models;
using ScanVault.Api.Repositories;
using ScanVault.Api.Storage;

namespace ScanVault.Api.Services;

/// <summary>The file part of an upload, kept independent of ASP.NET so the service is easy to test.</summary>
public record ScanUpload(Stream Content, string FileName, long Length, string Title, string? Notes, DateTime? CapturedAt);

/// <summary>An opened scan file, ready to be streamed back to the browser.</summary>
public record ScanFile(Stream Content, string ContentType, string FileName);

public interface IScanService
{
    Task<List<ScanDto>> GetBySiteAsync(int siteId);
    Task<ScanDto> GetAsync(int id);
    Task<ScanDto> UploadAsync(int siteId, ScanUpload upload);
    Task<ScanFile> OpenFileAsync(int id);
    Task DeleteAsync(int id);
}

public class ScanService : IScanService
{
    public const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    // Supported formats: .glb is a single-file 3D model (glTF binary); photos for 2D captures.
    private static readonly Dictionary<string, (ScanFormat Format, string ContentType)> AllowedTypes = new()
    {
        [".glb"] = (ScanFormat.Model3D, "model/gltf-binary"),
        [".jpg"] = (ScanFormat.Image, "image/jpeg"),
        [".jpeg"] = (ScanFormat.Image, "image/jpeg"),
        [".png"] = (ScanFormat.Image, "image/png")
    };

    private readonly IScanRepository _scans;
    private readonly ISiteRepository _sites;
    private readonly IFileStorage _storage;

    public ScanService(IScanRepository scans, ISiteRepository sites, IFileStorage storage)
    {
        _scans = scans;
        _sites = sites;
        _storage = storage;
    }

    public async Task<List<ScanDto>> GetBySiteAsync(int siteId)
    {
        await EnsureSiteExistsAsync(siteId);
        var scans = await _scans.GetBySiteAsync(siteId);
        return scans.Select(s => s.ToDto()).ToList();
    }

    public async Task<ScanDto> GetAsync(int id) => (await FindAsync(id)).ToDto();

    public async Task<ScanDto> UploadAsync(int siteId, ScanUpload upload)
    {
        await EnsureSiteExistsAsync(siteId);

        // 1. Validate the file before touching storage.
        var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
        if (!AllowedTypes.TryGetValue(extension, out var type))
            throw new BadRequestException("Unsupported file type. Upload a .glb 3D model or a .jpg/.png image.");
        if (upload.Length <= 0)
            throw new BadRequestException("The uploaded file is empty.");
        if (upload.Length > MaxFileSizeBytes)
            throw new BadRequestException("The file is larger than the 50 MB limit.");

        // 2. Store the file (Azure Blob Storage in the cloud), get back its key.
        var key = await _storage.SaveAsync(upload.Content, upload.FileName, type.ContentType);

        // 3. Save only the metadata in SQL.
        var scan = new Scan
        {
            SiteId = siteId,
            Title = upload.Title.Trim(),
            Notes = upload.Notes?.Trim(),
            CapturedAt = upload.CapturedAt?.ToUniversalTime() ?? DateTime.UtcNow,
            Format = type.Format,
            OriginalFileName = Path.GetFileName(upload.FileName),
            ContentType = type.ContentType,
            SizeBytes = upload.Length,
            StorageKey = key
        };

        try
        {
            await _scans.AddAsync(scan);
            await _scans.SaveChangesAsync();
        }
        catch
        {
            // If the database save fails, remove the file so storage and SQL stay consistent.
            await _storage.DeleteAsync(key);
            throw;
        }

        return scan.ToDto();
    }

    public async Task<ScanFile> OpenFileAsync(int id)
    {
        var scan = await FindAsync(id);
        var stream = await _storage.OpenReadAsync(scan.StorageKey)
                     ?? throw new NotFoundException($"The file for scan {id} is missing.");
        return new ScanFile(stream, scan.ContentType, scan.OriginalFileName);
    }

    public async Task DeleteAsync(int id)
    {
        var scan = await FindAsync(id);
        _scans.Remove(scan);
        await _scans.SaveChangesAsync();
        await _storage.DeleteAsync(scan.StorageKey);
    }

    private async Task<Scan> FindAsync(int id) =>
        await _scans.GetWithAnnotationsAsync(id)
        ?? throw new NotFoundException($"Scan {id} was not found.");

    private async Task EnsureSiteExistsAsync(int siteId)
    {
        if (await _sites.GetByIdAsync(siteId) is null)
            throw new NotFoundException($"Site {siteId} was not found.");
    }
}
