using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure;

namespace TranzrMoves.Infrastructure.Respositories;

public class LegalDocumentRepository(
    TranzrMovesDbContext dbContext,
    ILogger<LegalDocumentRepository> logger) : ILegalDocumentRepository
{
    public async Task<ErrorOr<LegalDocument>> CreateAsync(LegalDocument document, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.LegalDocuments.Add(document);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully created legal document {DocumentType} with ID {Id}", 
                document.DocumentType, document.Id);

            return document;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency conflict while creating legal document {DocumentType}", 
                document.DocumentType);
            return Error.Custom((int)CustomErrorType.Conflict, "LegalDocument.ConcurrencyConflict", 
                "A concurrency conflict occurred while creating the document");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating legal document {DocumentType}", document.DocumentType);
            return Error.Custom((int)CustomErrorType.InternalServerError, "LegalDocument.CreationError", 
                "An error occurred while creating the legal document");
        }
    }

    public async Task<LegalDocument?> GetCurrentAsync(LegalDocumentType documentType, DateTimeOffset asOfDate, CancellationToken cancellationToken)
    {
        try
        {
            var document = await dbContext.LegalDocuments
                .Where(d => d.DocumentType == documentType)
                .Where(d => d.IsActive)
                .Where(d => d.EffectiveFrom <= asOfDate)
                .Where(d => d.EffectiveTo == null || d.EffectiveTo >= asOfDate)
                .OrderByDescending(d => d.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);

            return document;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving current legal document {DocumentType} for date {AsOfDate}", 
                documentType, asOfDate);
            throw;
        }
    }

    public async Task<LegalDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var document = await dbContext.LegalDocuments
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            return document;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving legal document by ID {Id}", id);
            throw;
        }
    }

    public async Task<List<LegalDocument>> GetHistoryAsync(LegalDocumentType documentType, CancellationToken cancellationToken)
    {
        try
        {
            var documents = await dbContext.LegalDocuments
                .Where(d => d.DocumentType == documentType)
                .OrderByDescending(d => d.EffectiveFrom)
                .ToListAsync(cancellationToken);

            return documents;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving legal document history for {DocumentType}", documentType);
            throw;
        }
    }

    public async Task<ErrorOr<LegalDocument>> UpdateAsync(LegalDocument document, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.LegalDocuments.Update(document);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully updated legal document {DocumentType} with ID {Id}", 
                document.DocumentType, document.Id);

            return document;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency conflict while updating legal document {DocumentType} with ID {Id}", 
                document.DocumentType, document.Id);
            return Error.Custom((int)CustomErrorType.Conflict, "LegalDocument.ConcurrencyConflict", 
                "A concurrency conflict occurred while updating the document");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating legal document {DocumentType} with ID {Id}", 
                document.DocumentType, document.Id);
            return Error.Custom((int)CustomErrorType.InternalServerError, "LegalDocument.UpdateError", 
                "An error occurred while updating the legal document");
        }
    }

    public async Task<ErrorOr<bool>> ExpirePreviousDocumentsAsync(LegalDocumentType documentType, DateTimeOffset expireAt, CancellationToken cancellationToken)
    {
        try
        {
            var documentsToExpire = await dbContext.LegalDocuments
                .Where(d => d.DocumentType == documentType)
                .Where(d => d.IsActive)
                .Where(d => d.EffectiveTo == null)
                .ToListAsync(cancellationToken);

            foreach (var document in documentsToExpire)
            {
                document.EffectiveTo = expireAt;
                document.ModifiedAt = DateTimeOffset.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully expired {Count} previous documents for {DocumentType} at {ExpireAt}", 
                documentsToExpire.Count, documentType, expireAt);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error expiring previous documents for {DocumentType} at {ExpireAt}", 
                documentType, expireAt);
            return Error.Custom((int)CustomErrorType.InternalServerError, "LegalDocument.ExpirationError", 
                "An error occurred while expiring previous documents");
        }
    }
}
