using ErrorOr;

namespace TranzrMoves.Domain.Interfaces;

public interface ITurnstileService
{
    Task<ErrorOr<bool>> ValidateTokenAsync(string token, string? remoteIp = null, CancellationToken cancellationToken = default);
}
