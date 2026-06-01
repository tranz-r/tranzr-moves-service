namespace TranzrMoves.Domain.Interfaces;

public interface IBalanceChargeScheduler
{
    Task SchedulePayLaterAsync(Guid quoteId, LocalDate dueDate, string quoteReference,
        CancellationToken cancellationToken = default);

    Task ScheduleDepositBalanceAsync(Guid quoteId, LocalDate collectionDate, string quoteReference,
        CancellationToken cancellationToken = default);
}
