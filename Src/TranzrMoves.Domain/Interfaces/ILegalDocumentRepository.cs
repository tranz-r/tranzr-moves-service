using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface ILegalDocumentRepository
{
    Task<ErrorOr<LegalDocument>> CreateAsync(LegalDocument document, CancellationToken cancellationToken);
    Task<LegalDocument?> GetCurrentAsync(LegalDocumentType documentType, DateTimeOffset asOfDate, CancellationToken cancellationToken);
    Task<LegalDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<LegalDocument>> GetHistoryAsync(LegalDocumentType documentType, CancellationToken cancellationToken);
    Task<ErrorOr<LegalDocument>> UpdateAsync(LegalDocument document, CancellationToken cancellationToken);
    Task<ErrorOr<bool>> ExpirePreviousDocumentsAsync(LegalDocumentType documentType, DateTimeOffset expireAt, CancellationToken cancellationToken);
}
