using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.LegalDocuments.Get;

public record GetLegalDocumentQuery(GetLegalDocumentRequest Request) 
    : IRequest<ErrorOr<GetLegalDocumentResponse>>;
