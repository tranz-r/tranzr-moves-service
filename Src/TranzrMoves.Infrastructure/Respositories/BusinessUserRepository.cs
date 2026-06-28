using EntityFramework.Exceptions.Common;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class BusinessUserRepository(
    TranzrMovesDbContext dbContext,
    ILogger<BusinessUserRepository> logger) : IBusinessUserRepository
{
    public async Task<BusinessUser?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.Set<BusinessUser>()
            .AsNoTracking()
            .Include(x => x.BusinessAccount)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task<BusinessUser?> GetBySupabaseIdAsync(Guid supabaseId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<UserV2>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SupabaseId == supabaseId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        // Identity resolution must not be tenant-scoped (the tenant is resolved
        // from this very lookup), so bypass the global query filter.
        var businessUser = await dbContext.Set<BusinessUser>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(x => x.BusinessAccount)
            .FirstOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);

        // Reuse the user we already loaded so callers can read the profile (first/last name)
        // without an extra join.
        if (businessUser is not null)
        {
            businessUser.User = user;
        }

        return businessUser;
    }

    public async Task<IReadOnlyList<BusinessUser>> GetByBusinessAccountIdAsync(
        Guid businessAccountId,
        CancellationToken cancellationToken)
        => await dbContext.Set<BusinessUser>()
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.BusinessAccountId == businessAccountId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<BusinessUser?> GetByIdAsync(Guid businessUserId, CancellationToken cancellationToken)
        => await dbContext.Set<BusinessUser>()
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.BusinessAccount)
            .FirstOrDefaultAsync(x => x.Id == businessUserId, cancellationToken);

    public async Task<BusinessUser?> GetByUserIdGlobalAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.Set<BusinessUser>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task<ErrorOr<BusinessUser>> InviteAsync(
        UserV2 user,
        bool userIsNew,
        BusinessUser businessUser,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (userIsNew)
            {
                dbContext.Set<UserV2>().Add(user);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            dbContext.Set<BusinessUser>().Add(businessUser);
            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return businessUser;
        }
        catch (UniqueConstraintException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Unique constraint violated inviting business user for {UserId}", user.Id);
            return Error.Conflict(
                code: "BusinessUser.InviteConflict",
                description: "This user already belongs to a business account.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to invite business user for {UserId}", user.Id);
            return Error.Failure(
                code: "BusinessUser.InviteFailed",
                description: "Failed to create the business user invitation.");
        }
    }

    public async Task<ErrorOr<BusinessUser>> UpdateStatusAsync(
        Guid businessUserId,
        BusinessUserStatus status,
        CancellationToken cancellationToken)
    {
        var tracked = await dbContext.Set<BusinessUser>()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == businessUserId, cancellationToken);

        if (tracked is null)
        {
            return Error.NotFound(
                code: "BusinessUser.NotFound",
                description: "Business user not found.");
        }

        tracked.Status = status;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception updating business user {BusinessUserId}", businessUserId);
            return Error.Conflict();
        }

        return tracked;
    }

    public async Task<ErrorOr<BusinessUser>> AcceptInvitationAsync(
        Guid userId,
        Guid supabaseId,
        Guid businessUserId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var user = await dbContext.Set<UserV2>()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user is null)
            {
                return Error.NotFound(
                    code: "Auth.InvitationNotFound",
                    description: "No pending invitation was found for this account.");
            }

            // Membership lookup must bypass the tenant filter: no tenant is established
            // for an invitee until acceptance completes.
            var businessUser = await dbContext.Set<BusinessUser>()
                .AsTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == businessUserId, cancellationToken);

            if (businessUser is null)
            {
                return Error.NotFound(
                    code: "Auth.InvitationNotFound",
                    description: "No pending invitation was found for this account.");
            }

            user.SupabaseId = supabaseId;
            businessUser.Status = BusinessUserStatus.Active;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return businessUser;
        }
        catch (UniqueConstraintException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Unique constraint violated accepting invitation for {UserId}", userId);
            return Error.Conflict(
                code: "Auth.EmailLinkedToAnotherAccount",
                description: "This email is already linked to another auth account.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Concurrency exception accepting invitation for business user {BusinessUserId}", businessUserId);
            return Error.Conflict();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to accept invitation for business user {BusinessUserId}", businessUserId);
            return Error.Failure(
                code: "Auth.AcceptInvitationFailed",
                description: "Failed to accept the invitation.");
        }
    }
}
