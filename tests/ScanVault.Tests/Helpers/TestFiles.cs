using ScanVault.Api.Services;

namespace ScanVault.Tests.Helpers;

/// <summary>Builds small fake uploads for tests.</summary>
public static class TestFiles
{
    public static ScanUpload Glb(string title, DateTime? capturedAt = null) =>
        Make("scan.glb", title, capturedAt);

    public static ScanUpload Png(string title) => Make("photo.png", title, null);

    public static ScanUpload Make(string fileName, string title, DateTime? capturedAt, int size = 64)
    {
        var bytes = Enumerable.Range(0, size).Select(i => (byte)i).ToArray();
        return new ScanUpload(new MemoryStream(bytes), fileName, bytes.Length, title, null, capturedAt);
    }
}
