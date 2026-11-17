namespace ScanVault.Api.Storage;

/// <summary>
/// Abstraction over "where scan files live" (polymorphism).
/// LocalFileStorage writes to disk for development; AzureBlobFileStorage writes to Azure Blob Storage.
/// The rest of the app only knows this interface, so switching is a config change.
/// </summary>
public interface IFileStorage
{
    /// <summary>Saves the stream and returns the key used to find it again.</summary>
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Opens a stored file for reading, or returns null if it does not exist.</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);
}

public static class StorageKeys
{
    /// <summary>Builds a unique, collision-free key such as "2026/11/3f2c...e1.glb".</summary>
    public static string Create(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var now = DateTime.UtcNow;
        return $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";
    }
}
