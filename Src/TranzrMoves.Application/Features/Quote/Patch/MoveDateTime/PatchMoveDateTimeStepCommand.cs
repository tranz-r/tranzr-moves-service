using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Features.Quote.Patch.MoveDateTime;

public sealed record PatchMoveDateTimeStepCommand : ICommand<ErrorOr<QuoteJourneyResponse>>
{
    public Guid QuoteId { get; set; }
    public uint ExpectedVersion { get; set; }
    public Schedule Schedule { get; set; } = default!;
    public int SelectedVanCount { get; set; }
}
