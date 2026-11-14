using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken cancellationToken = default);
}

public sealed class BlobStorageService(IConfiguration configuration) : IBlobStorageService
{
    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken cancellationToken = default)
    {
        var connectionString = configuration["AzureBlobStorage:ConnectionString"]
            ?? configuration["BlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure Blob Storage is not configured.");

        var container = configuration["AzureBlobStorage:ContainerName"]
            ?? configuration["BlobStorage:ContainerName"]
            ?? "raceday-images";
        var containerClient = new BlobContainerClient(connectionString, container);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        var blobName = $"{folder.Trim('/')}/{Guid.NewGuid():N}{Path.GetExtension(fileName).ToLowerInvariant()}";
        var blob = containerClient.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
        return blob.Uri.ToString();
    }
}
