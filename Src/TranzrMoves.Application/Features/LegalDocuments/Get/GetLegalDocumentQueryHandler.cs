using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.LegalDocuments.Get;

public class GetLegalDocumentQueryHandler(
    ILegalDocumentRepository legalDocumentRepository,
    IAzureBlobService azureBlobService,
    ILogger<GetLegalDocumentQueryHandler> logger)
    : IRequestHandler<GetLegalDocumentQuery, ErrorOr<GetLegalDocumentResponse>>
{
    public async ValueTask<ErrorOr<GetLegalDocumentResponse>> Handle(
        GetLegalDocumentQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = query.Request;
            var asOfDate = request.AsOfDate ?? DateTimeOffset.UtcNow;

            // Get current document from database
            var document = await legalDocumentRepository.GetCurrentAsync(
                request.DocumentType, asOfDate, cancellationToken);

            if (document == null)
            {
                logger.LogWarning("No {DocumentType} found for date {AsOfDate}", 
                    request.DocumentType, asOfDate);
                return Error.Custom((int)CustomErrorType.NotFound, "LegalDocument.NotFound", 
                    $"No {request.DocumentType} document found for the specified date");
            }

            // Download content from Azure Blob Storage
            var downloadResult = await azureBlobService.DownloadBlobAsync(
                document.ContainerName, document.BlobName, cancellationToken);

            if (downloadResult.IsError)
            {
                logger.LogError("Failed to download blob {BlobName}: {Error}", 
                    document.BlobName, downloadResult.FirstError.Description);
                return downloadResult.Errors;
            }

            logger.LogInformation("Successfully retrieved {DocumentType} version {Version} for date {AsOfDate}", 
                request.DocumentType, document.Version, asOfDate);

            return new GetLegalDocumentResponse(
                document.Id,
                document.DocumentType,
                downloadResult.Value,
                document.Version,
                document.EffectiveFrom,
                document.EffectiveTo,
                document.CreatedAt,
                document.CreatedBy);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving legal document {DocumentType} for date {AsOfDate}", 
                query.Request.DocumentType, query.Request.AsOfDate);
            return Error.Custom((int)CustomErrorType.InternalServerError, "LegalDocument.RetrievalError", 
                "An error occurred while retrieving the legal document");
        }
    }
}
