using ScanVault.Api.Dtos;
using ScanVault.Api.Errors;
using ScanVault.Api.Models;
using ScanVault.Api.Repositories;

namespace ScanVault.Api.Services;

public interface IAnnotationService
{
    Task<List<AnnotationDto>> GetByScanAsync(int scanId);
    Task<AnnotationDto> CreateAsync(int scanId, CreateAnnotationRequest request);
    Task DeleteAsync(int id);
}

public class AnnotationService : IAnnotationService
{
    private readonly IAnnotationRepository _annotations;
    private readonly IScanRepository _scans;

    public AnnotationService(IAnnotationRepository annotations, IScanRepository scans)
    {
        _annotations = annotations;
        _scans = scans;
    }

    public async Task<List<AnnotationDto>> GetByScanAsync(int scanId)
    {
        await EnsureScanExistsAsync(scanId);
        var items = await _annotations.GetByScanAsync(scanId);
        return items.Select(a => a.ToDto()).ToList();
    }

    public async Task<AnnotationDto> CreateAsync(int scanId, CreateAnnotationRequest request)
    {
        var scan = await EnsureScanExistsAsync(scanId);

        // Photos use normalised coordinates: X and Y between 0 and 1, Z always 0.
        if (scan.Format == ScanFormat.Image &&
            (request.X is < 0 or > 1 || request.Y is < 0 or > 1 || request.Z != 0))
            throw new BadRequestException("Image annotations need X and Y between 0 and 1, and Z = 0.");

        if (!Enum.IsDefined(request.Category))
            throw new BadRequestException("Unknown annotation category.");

        var annotation = new Annotation
        {
            ScanId = scanId,
            Text = request.Text.Trim(),
            Category = request.Category,
            X = request.X,
            Y = request.Y,
            Z = request.Z
        };

        await _annotations.AddAsync(annotation);
        await _annotations.SaveChangesAsync();
        return annotation.ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        var annotation = await _annotations.GetByIdAsync(id)
                         ?? throw new NotFoundException($"Annotation {id} was not found.");
        _annotations.Remove(annotation);
        await _annotations.SaveChangesAsync();
    }

    private async Task<Scan> EnsureScanExistsAsync(int scanId) =>
        await _scans.GetByIdAsync(scanId)
        ?? throw new NotFoundException($"Scan {scanId} was not found.");
}
