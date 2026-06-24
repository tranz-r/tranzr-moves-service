using EntityFramework.Exceptions.Common;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class BusinessAccountRepository(
    TranzrMovesDbContext dbContext,
    ILogger<BusinessAccountRepository> logger) : IBusinessAccountRepository
{
    public async Task<BusinessAccount?> GetByIdAsync(Guid businessAccountId, CancellationToken cancellationToken)
        => await dbContext.Set<BusinessAccount>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == businessAccountId, cancellationToken);

    public async Task<ErrorOr<BusinessAccount>> UpdateAsync(
        BusinessAccount businessAccount,
        CancellationToken cancellationToken)
    {
        var tracked = await dbContext.Set<BusinessAccount>()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == businessAccount.Id, cancellationToken);

        if (tracked is null)
        {
            return Error.NotFound(
                code: "BusinessAccount.NotFound",
                description: "Business account not found.");
        }

        tracked.BusinessName = businessAccount.BusinessName;
        tracked.TradingName = businessAccount.TradingName;
        tracked.BusinessEmail = businessAccount.BusinessEmail;
        tracked.BusinessPhone = businessAccount.BusinessPhone;
        tracked.CompanyRegistrationNumber = businessAccount.CompanyRegistrationNumber;
        tracked.VatNumber = businessAccount.VatNumber;
        tracked.Status = businessAccount.Status;
        tracked.BillingAddress = businessAccount.BillingAddress;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception updating business account {BusinessAccountId}",
                businessAccount.Id);
            return Error.Conflict();
        }
        catch (UniqueConstraintException ex)
        {
            logger.LogError(ex, "Unique constraint violated updating business account {BusinessAccountId}",
                businessAccount.Id);
            return Error.Conflict(
                code: "BusinessAccount.EmailAlreadyExists",
                description: "A business account with this email already exists.");
        }

        return tracked;
    }

    public async Task<ErrorOr<BusinessAccount>> SuspendAsync(
        Guid businessAccountId,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Set<BusinessAccount>()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == businessAccountId, cancellationToken);

        if (account is null)
        {
            return Error.NotFound(
                code: "BusinessAccount.NotFound",
                description: "Business account not found.");
        }

        if (account.Status == BusinessAccountStatus.Closed)
        {
            return Error.Validation(
                code: "BusinessAccount.Closed",
                description: "Cannot suspend a closed business account.");
        }

        account.Status = BusinessAccountStatus.Suspended;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception suspending business account {BusinessAccountId}",
                businessAccountId);
            return Error.Conflict();
        }

        return account;
    }

    public async Task<ErrorOr<BusinessAccount>> ActivateAsync(
        Guid businessAccountId,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Set<BusinessAccount>()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == businessAccountId, cancellationToken);

        if (account is null)
        {
            return Error.NotFound(
                code: "BusinessAccount.NotFound",
                description: "Business account not found.");
        }

        if (account.Status == BusinessAccountStatus.Closed)
        {
            return Error.Validation(
                code: "BusinessAccount.Closed",
                description: "Cannot activate a closed business account.");
        }

        account.Status = BusinessAccountStatus.Active;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception activating business account {BusinessAccountId}",
                businessAccountId);
            return Error.Conflict();
        }

        return account;
    }

    public async Task<ErrorOr<RegisterBusinessAccountResult>> RegisterAsync(
        UserV2 user,
        BusinessAccount businessAccount,
        BusinessUser businessUser,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            dbContext.Set<UserV2>().Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.Set<BusinessAccount>().Add(businessAccount);
            await dbContext.SaveChangesAsync(cancellationToken);

            businessUser.BusinessAccountId = businessAccount.Id;
            businessUser.UserId = user.Id;
            dbContext.Set<BusinessUser>().Add(businessUser);
            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new RegisterBusinessAccountResult(
                user.Id,
                businessAccount.Id,
                businessUser.Id,
                businessUser.Role,
                businessUser.Status);
        }
        catch (UniqueConstraintException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Unique constraint violated during business account registration");
            return Error.Conflict(
                code: "BusinessAccount.RegistrationConflict",
                description: "A business account or user with these details already exists.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to register business account");
            return Error.Failure(
                code: "BusinessAccount.RegistrationFailed",
                description: "Business account registration failed.");
        }
    }
}
