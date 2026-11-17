namespace ScanVault.Api.Models;

/// <summary>
/// One capture of a site at a point in time: a 3D model (.glb) or a photo (.jpg/.png).
/// Only metadata lives in SQL; the file itself lives in file storage (Azure Blob Storage in the cloud).
/// </summary>
public class Scan : EntityBase
{
    public int SiteId { get; set; }
    public Site? Site { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }

    /// <summary>When the scan was captured on site (used to build the site's history).</summary>
    public DateTime CapturedAt { get; set; }

    public ScanFormat Format { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    /// <summary>Key of the file inside file storage (the blob name in Azure).</summary>
    public string StorageKey { get; set; } = string.Empty;

    public List<Annotation> Annotations { get; set; } = new();
}
