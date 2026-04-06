using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class DriverQuote : IAuditable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;
    public Instant CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public Instant ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}
