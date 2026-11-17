using System.ComponentModel.DataAnnotations;
using ScanVault.Api.Models;

namespace ScanVault.Api.Dtos;

// DTOs (Data Transfer Objects) are the shapes the API sends and receives.
// They keep database entities private and let us validate input with attributes.
// [ApiController] automatically returns 400 Bad Request when validation fails.

// ---------- Sites ----------

public record CreateSiteRequest(
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    [StringLength(200)] string? Location,
    [StringLength(1000)] string? Description);

public record UpdateSiteRequest(
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    [StringLength(200)] string? Location,
    [StringLength(1000)] string? Description);

public record SiteSummaryDto(
    int Id,
    string Name,
    string? Location,
    int ScanCount,
    DateTime? LastCapturedAt,
    DateTime CreatedAt);

public record SiteDetailDto(
    int Id,
    string Name,
    string? Location,
    string? Description,
    DateTime CreatedAt,
    List<ScanDto> Scans);

// ---------- Scans ----------

public class UploadScanRequest
{
    [Required]
    public IFormFile File { get; set; } = default!;

    [Required, StringLength(150, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>When the scan was captured. Defaults to now if not given.</summary>
    public DateTime? CapturedAt { get; set; }
}

public record ScanDto(
    int Id,
    int SiteId,
    string Title,
    string? Notes,
    ScanFormat Format,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTime CapturedAt,
    DateTime CreatedAt,
    int AnnotationCount,
    string FileUrl);

// ---------- Annotations ----------

public record CreateAnnotationRequest(
    [Required, StringLength(500, MinimumLength = 1)] string Text,
    AnnotationCategory Category,
    double X,
    double Y,
    double Z);

public record AnnotationDto(
    int Id,
    int ScanId,
    string Text,
    AnnotationCategory Category,
    double X,
    double Y,
    double Z,
    DateTime CreatedAt);
