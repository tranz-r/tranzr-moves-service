using Mediator;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Application.Features.Checkout.PaymentSheet;

public sealed record CreateQuoteV2PaymentSheetCommand(
    Guid QuoteId,
    uint ExpectedVersion,
    PaymentType PaymentType) : ICommand<ErrorOr<QuoteV2PaymentSheetResult>>;
