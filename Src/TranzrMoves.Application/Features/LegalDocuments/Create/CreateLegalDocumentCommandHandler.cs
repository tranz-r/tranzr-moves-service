using System.Security.Cryptography;
using System.Text;
using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.LegalDocuments.Create;

public class CreateLegalDocumentCommandHandler(
    ILegalDocumentRepository legalDocumentRepository,
    IAzureBlobService azureBlobService,
    ILogger<CreateLegalDocumentCommandHandler> logger) 
    : IRequestHandler<CreateLegalDocumentCommand, ErrorOr<CreateLegalDocumentResponse>>
{
    private const string ContainerName = "legal";
    
    public async ValueTask<ErrorOr<CreateLegalDocumentResponse>> Handle(
        CreateLegalDocumentCommand command, 
        CancellationToken cancellationToken)
    {
        try
        {
            var request = command.Request;
            
            // Validate content
            if (string.IsNullOrWhiteSpace(request.MarkdownContent))
            {
                return Error.Custom((int)CustomErrorType.BadRequest, "LegalDocument.InvalidContent", 
                    "Markdown content cannot be empty");
            }

            // Generate version hash
            var contentBytes = Encoding.UTF8.GetBytes(request.MarkdownContent);
            var version = GenerateVersionHash(contentBytes);
            
            // Generate blob name with timestamp and version
            var now = DateTimeOffset.UtcNow;
            var effectiveFrom = new DateTimeOffset(now.Date.AddDays(1), TimeSpan.Zero); // Tomorrow UTC midnight
            var blobName = GenerateBlobName(request.DocumentType, effectiveFrom, version);
            
            // Upload to Azure Blob Storage
            var uploadResult = await azureBlobService.UploadBlobAsync(ContainerName, blobName, request.MarkdownContent, cancellationToken);
            if (uploadResult.IsError)
            {
                logger.LogError("Failed to upload blob {BlobName}: {Error}", blobName, uploadResult.FirstError.Description);
                return uploadResult.Errors;
            }

            // Create document entity
            var document = new LegalDocument
            {
                Id = Guid.NewGuid(),
                DocumentType = request.DocumentType,
                BlobName = blobName,
                ContainerName = ContainerName,
                Version = version,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = null, // Will be set when a new version is created
                IsActive = true,
                ContentLength = contentBytes.Length,
                ContentHash = GenerateContentHash(contentBytes)
            };

            // Save to database
            var createResult = await legalDocumentRepository.CreateAsync(document, cancellationToken);
            if (createResult.IsError)
            {
                // Cleanup: delete the uploaded blob
                await azureBlobService.DeleteBlobAsync(ContainerName, blobName, cancellationToken);
                logger.LogError("Failed to save document to database: {Error}", createResult.FirstError.Description);
                return createResult.Errors;
            }

            // Expire previous documents at midnight tonight
            var expireAt = new DateTimeOffset(now.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero); // Tonight UTC 23:59:59
            var expireResult = await legalDocumentRepository.ExpirePreviousDocumentsAsync(
                request.DocumentType, expireAt, cancellationToken);
            
            if (expireResult.IsError)
            {
                logger.LogWarning("Failed to expire previous documents: {Error}", expireResult.FirstError.Description);
                // Continue anyway - the new document was created successfully
            }

            logger.LogInformation("Successfully created legal document {DocumentType} with version {Version}, effective from {EffectiveFrom}", 
                request.DocumentType, version, effectiveFrom);

            return new CreateLegalDocumentResponse(
                document.Id,
                document.DocumentType,
                document.Version,
                document.EffectiveFrom,
                document.EffectiveTo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating legal document {DocumentType}", command.Request.DocumentType);
            return Error.Custom((int)CustomErrorType.InternalServerError, "LegalDocument.CreationError", 
                "An error occurred while creating the legal document");
        }
    }

    private static string GenerateVersionHash(byte[] content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(content);
        return Convert.ToHexString(hash)[..12].ToLowerInvariant(); // First 12 characters
    }

    private static string GenerateContentHash(byte[] content)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(content);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GenerateBlobName(LegalDocumentType documentType, DateTimeOffset effectiveDate, string version)
    {
        var prefix = documentType switch
        {
            LegalDocumentType.TermsAndConditions => "legal/terms-and-conditions",
            LegalDocumentType.PrivacyPolicy => "legal/privacy-policy",
            _ => throw new ArgumentOutOfRangeException(nameof(documentType))
        };

        var dateString = effectiveDate.ToString("yyyy-MM-dd");
        return $"{prefix}/{dateString}/{version}.md";
    }
}
