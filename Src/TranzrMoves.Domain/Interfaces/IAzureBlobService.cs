using ErrorOr;

namespace TranzrMoves.Domain.Interfaces;

public interface IAzureBlobService
{
    Task<ErrorOr<string>> UploadBlobAsync(string containerName, string blobName, string content, CancellationToken cancellationToken = default);
    Task<ErrorOr<string>> DownloadBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<ErrorOr<bool>> DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<ErrorOr<bool>> BlobExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<ErrorOr<(string contentHash, int contentLength)>> GetBlobMetadataAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
}
