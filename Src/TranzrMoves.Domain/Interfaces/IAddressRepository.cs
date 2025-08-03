using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IAddressRepository
{
    Task<ErrorOr<Address>> AddAddressAsync(Address address,
        CancellationToken cancellationToken);

    Task<Address?> GetAddressAsync(Guid addressId, CancellationToken cancellationToken);

    Task<ErrorOr<Address>> UpdateAddressAsync(Address address,
        CancellationToken cancellationToken);

    Task DeleteAddressAsync(Address address, CancellationToken cancellationToken);
}