using EntityFramework.Exceptions.Common;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class AddressRepository(TranzrMovesDbContext dbContext, ILogger<AddressRepository> logger) : IAddressRepository
{
    public async Task<ErrorOr<Address>> AddAddressAsync(Address address,
        CancellationToken cancellationToken)
    {
        dbContext.Set<Address>().Add(address);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (CannotInsertNullException e)
        {
            logger.LogError("Cannot insert null value for {property}", e.Source);
            return Error.Custom(
                type: (int)CustomErrorType.BadRequest,
                code: "Null.Value",
                description: "Cannot insert null value");
        }
        catch (UniqueConstraintException e)
        {
            logger.LogError("Unique constraint {constraintName} violated. Duplicate value for {constraintProperty}",
                e.ConstraintName, e.ConstraintProperties[0]);
            return Error.Conflict();
        }

        return address;
    }

    public async Task<Address?> GetAddressAsync(Guid addressId, CancellationToken cancellationToken)
        => await dbContext.Set<Address>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == addressId, cancellationToken);


    public async Task<ErrorOr<Address>> UpdateAddressAsync(Address address,
        CancellationToken cancellationToken)
    {
        dbContext.Set<Address>().Update(address);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception occurred while updating Address with AddressId {AddressId}",
                address.Id);
            return Error.Conflict();
        }

        return address;
    }

    public async Task DeleteAddressAsync(Address address, CancellationToken cancellationToken)
        => await dbContext.Set<Address>()
            .Where(ac => ac.Id == address.Id)
            .ExecuteDeleteAsync(cancellationToken);
}