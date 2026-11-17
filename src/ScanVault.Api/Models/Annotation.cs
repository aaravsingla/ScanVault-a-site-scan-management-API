namespace ScanVault.Api.Models;

/// <summary>
/// A note pinned to an exact location on a scan, e.g. "crack in this wall".
/// For 3D models X/Y/Z is a point on the model; for photos X/Y are image coordinates (0-1) and Z is 0.
/// </summary>
public class Annotation : EntityBase
{
    public int ScanId { get; set; }
    public Scan? Scan { get; set; }

    public string Text { get; set; } = string.Empty;
    public AnnotationCategory Category { get; set; } = AnnotationCategory.Note;

    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}
