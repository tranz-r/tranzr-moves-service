using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class QuoteSession : IAuditable
{
    public string SessionId { get; set; } = string.Empty; // guestId (PK)
    
    // Container for multiple quotes
    public List<Quote> Quotes { get; set; } = [];
    
    // Session metadata
    public string ETag { get; set; } = string.Empty; // W/"<sha256>"
    public DateTimeOffset? ExpiresUtc { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}




