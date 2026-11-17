using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ScanVault.Api.Storage;

/// <summary>
/// Stores scan files in an Azure Blob Storage container.
/// SQL keeps only the metadata and the blob name (StorageKey); the bytes live here.
/// </summary>
public class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobFileStorage(BlobContainerClient container)
    {
        _container = container;
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var key = StorageKeys.Create(fileName);
        var blob = _container.GetBlobClient(key);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);

        return key;
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await _container.GetBlobClient(key).OpenReadAsync(cancellationToken: ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public Task DeleteAsync(string key, CancellationToken ct = default) =>
        _container.GetBlobClient(key).DeleteIfExistsAsync(cancellationToken: ct);
}
