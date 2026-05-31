using ErrorOr;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.IntegrationTests.TestDoubles;

internal sealed class NoOpTurnstileService : ITurnstileService
{
    public Task<ErrorOr<bool>> ValidateTokenAsync(string token, string? remoteIp = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ErrorOr<bool>>(true);
}
