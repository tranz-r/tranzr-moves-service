using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class PricingTier : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
}