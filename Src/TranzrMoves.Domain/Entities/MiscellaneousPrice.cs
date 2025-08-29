using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public enum MiscellaneousType
{
    Dismantle = 1,
    Assembly = 2,
    Storage = 3
}

public class MiscellaneousPrice : IAuditable
{
    public Guid Id { get; set; }
    public MiscellaneousType Type { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "GBP";
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
}