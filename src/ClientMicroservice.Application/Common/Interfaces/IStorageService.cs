namespace ClientMicroservice.Application.Common.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct);
}
