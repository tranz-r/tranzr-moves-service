using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.LegalDocuments.GetHistory;

public class GetLegalDocumentHistoryQueryHandler(
    ILegalDocumentRepository legalDocumentRepository,
    ILogger<GetLegalDocumentHistoryQueryHandler> logger)
    : IRequestHandler<GetLegalDocumentHistoryQuery, ErrorOr<GetLegalDocumentHistoryResponse>>
{
    public async ValueTask<ErrorOr<GetLegalDocumentHistoryResponse>> Handle(
        GetLegalDocumentHistoryQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var documents = await legalDocumentRepository.GetHistoryAsync(query.DocumentType, cancellationToken);
            
            var mapper = new LegalDocumentMapper();
            var documentDtos = mapper.ToDtoList(documents);

            logger.LogInformation("Successfully retrieved {Count} historical documents for {DocumentType}", 
                documents.Count, query.DocumentType);

            return new GetLegalDocumentHistoryResponse(documentDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving legal document history for {DocumentType}", query.DocumentType);
            return Error.Failure("LegalDocument.HistoryError", 
                "An error occurred while retrieving the legal document history");
        }
    }
}
