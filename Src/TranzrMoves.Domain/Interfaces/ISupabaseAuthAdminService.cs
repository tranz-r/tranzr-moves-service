using ErrorOr;

namespace TranzrMoves.Domain.Interfaces;

public interface ISupabaseAuthAdminService
{
    Task<ErrorOr<SupabaseAuthUser>> CreateUserAsync(
        SupabaseAuthUserCreateRequest request,
        CancellationToken cancellationToken);
}

public sealed record SupabaseAuthUserCreateRequest(
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber);

public sealed record SupabaseAuthUser(Guid Id, string Email);
