namespace TranzrMoves.Domain.Interfaces;

public interface IPayLaterChargeScheduler
{
    Task ScheduleAsync(Guid quoteId, LocalDate dueDate, string quoteReference, CancellationToken cancellationToken = default);
}
