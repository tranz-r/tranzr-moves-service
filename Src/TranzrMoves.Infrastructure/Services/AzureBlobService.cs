using System.Security.Cryptography;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ErrorOr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services;

public class AzureBlobService(
    IConfiguration configuration,
    ILogger<AzureBlobService> logger) : IAzureBlobService
{
    private readonly string _connectionString = configuration["AZURE_STORAGE_CONNECTION_STRING"] ?? 
        throw new InvalidOperationException("AZURE_STORAGE_CONNECTION_STRING is not configured");
    
    // private readonly string _sasToken = configuration["AZURE_STORAGE_SAS_TOKEN"] ?? 
    //     throw new InvalidOperationException("AZURE_STORAGE_SAS_TOKEN is not configured");

    public async Task<ErrorOr<string>> UploadBlobAsync(string containerName, string blobName, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var contentBytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(contentBytes);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = "text/markdown",
                ContentEncoding = "utf-8"
            };

            var metadata = new Dictionary<string, string>
            {
                { "ContentHash", GenerateContentHash(contentBytes) },
                { "ContentLength", contentBytes.Length.ToString() },
                { "UploadedAt", DateTimeOffset.UtcNow.ToString("O") }
            };

            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders,
                Metadata = metadata,
                Conditions = null
            }, cancellationToken);

            logger.LogInformation("Successfully uploaded blob {BlobName} to container {ContainerName}", 
                blobName, containerName);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload blob {BlobName} to container {ContainerName}", 
                blobName, containerName);
            return Error.Custom((int)CustomErrorType.ServiceUnavailable, "AzureBlob.UploadFailed", 
                $"Failed to upload blob: {ex.Message}");
        }
    }

    public async Task<ErrorOr<string>> DownloadBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadContentAsync(cancellationToken);
            var content = response.Value.Content.ToString();

            logger.LogInformation("Successfully downloaded blob {BlobName} from container {ContainerName}", 
                blobName, containerName);

            return content;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download blob {BlobName} from container {ContainerName}", 
                blobName, containerName);
            return Error.Custom((int)CustomErrorType.ServiceUnavailable, "AzureBlob.DownloadFailed", 
                $"Failed to download blob: {ex.Message}");
        }
    }

    public async Task<ErrorOr<bool>> DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            logger.LogInformation("Successfully deleted blob {BlobName} from container {ContainerName}", 
                blobName, containerName);

            return response.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete blob {BlobName} from container {ContainerName}", 
                blobName, containerName);
            return Error.Custom((int)CustomErrorType.ServiceUnavailable, "AzureBlob.DeleteFailed", 
                $"Failed to delete blob: {ex.Message}");
        }
    }

    public async Task<ErrorOr<bool>> BlobExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.ExistsAsync(cancellationToken);

            return response.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check if blob {BlobName} exists in container {ContainerName}", 
                blobName, containerName);
            return Error.Custom((int)CustomErrorType.ServiceUnavailable, "AzureBlob.ExistsCheckFailed", 
                $"Failed to check blob existence: {ex.Message}");
        }
    }

    public async Task<ErrorOr<(string contentHash, int contentLength)>> GetBlobMetadataAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            var contentHash = properties.Value.Metadata.TryGetValue("ContentHash", out var hash) ? hash : "";
            var contentLength = properties.Value.Metadata.TryGetValue("ContentLength", out var length) ? int.Parse(length) : 0;

            return (contentHash, contentLength);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get metadata for blob {BlobName} in container {ContainerName}", 
                blobName, containerName);
            return Error.Custom((int)CustomErrorType.ServiceUnavailable, "AzureBlob.MetadataFailed", 
                $"Failed to get blob metadata: {ex.Message}");
        }
    }

    private static string GenerateContentHash(byte[] content)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(content);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
