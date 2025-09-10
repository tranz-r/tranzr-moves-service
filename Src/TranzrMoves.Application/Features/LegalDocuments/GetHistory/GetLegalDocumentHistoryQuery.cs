using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Features.LegalDocuments.GetHistory;

public record GetLegalDocumentHistoryQuery(LegalDocumentType DocumentType) 
    : IRequest<ErrorOr<GetLegalDocumentHistoryResponse>>;
