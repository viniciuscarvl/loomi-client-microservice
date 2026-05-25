using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ClientMicroservice.Application.Common.Interfaces;

namespace ClientMicroservice.Infrastructure.Storage;

internal sealed class AzureBlobStorageService(BlobServiceClient blobServiceClient, string containerName)
    : IStorageService
{
    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobName = $"{Guid.NewGuid()}-{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: ct);

        return blobClient.Uri.ToString();
    }
}
