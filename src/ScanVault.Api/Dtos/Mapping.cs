using ScanVault.Api.Models;

namespace ScanVault.Api.Dtos;

/// <summary>Converts database entities into API DTOs (kept in one place on purpose).</summary>
public static class Mapping
{
    public static SiteSummaryDto ToSummaryDto(this Site site) => new(
        site.Id,
        site.Name,
        site.Location,
        site.Scans.Count,
        site.Scans.Count == 0 ? null : site.Scans.Max(s => s.CapturedAt),
        site.CreatedAt);

    public static SiteDetailDto ToDetailDto(this Site site) => new(
        site.Id,
        site.Name,
        site.Location,
        site.Description,
        site.CreatedAt,
        site.Scans.OrderByDescending(s => s.CapturedAt).Select(s => s.ToDto()).ToList());

    public static ScanDto ToDto(this Scan scan) => new(
        scan.Id,
        scan.SiteId,
        scan.Title,
        scan.Notes,
        scan.Format,
        scan.OriginalFileName,
        scan.ContentType,
        scan.SizeBytes,
        scan.CapturedAt,
        scan.CreatedAt,
        scan.Annotations.Count,
        $"/api/scans/{scan.Id}/file");

    public static AnnotationDto ToDto(this Annotation a) => new(
        a.Id, a.ScanId, a.Text, a.Category, a.X, a.Y, a.Z, a.CreatedAt);
}
