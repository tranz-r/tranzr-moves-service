using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class DriverQuote : IAuditable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; } 
    public User User { get; set; }
    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTimeOffset ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}