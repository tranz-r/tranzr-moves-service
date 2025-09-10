namespace TranzrMoves.Application.Contracts.DriverJobs;

public record UnassignDriverJobRequest(Guid DriverId, Guid QuoteId);
