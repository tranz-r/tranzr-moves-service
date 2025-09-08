using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.LegalDocuments.Create;

public record CreateLegalDocumentCommand(CreateLegalDocumentRequest Request) 
    : IRequest<ErrorOr<CreateLegalDocumentResponse>>;
