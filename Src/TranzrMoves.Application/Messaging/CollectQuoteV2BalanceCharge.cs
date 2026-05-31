namespace TranzrMoves.Application.Messaging;

public sealed record CollectQuoteV2BalanceCharge(Guid QuoteId, LocalDate DueDate);
