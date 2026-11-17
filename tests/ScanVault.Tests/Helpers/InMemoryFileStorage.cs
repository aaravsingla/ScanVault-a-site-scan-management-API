using ScanVault.Api.Storage;

namespace ScanVault.Tests.Helpers;

/// <summary>
/// A fake IFileStorage that keeps files in a dictionary.
/// Because services depend on the interface, tests can swap in this fake (no disk, no Azure).
/// </summary>
public class InMemoryFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = new();

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var key = StorageKeys.Create(fileName);
        Files[key] = buffer.ToArray();
        return key;
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default) =>
        Task.FromResult<Stream?>(Files.TryGetValue(key, out var bytes) ? new MemoryStream(bytes) : null);

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        Files.Remove(key);
        return Task.CompletedTask;
    }
}
