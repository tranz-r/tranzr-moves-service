using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class InventoryItem : IAuditable
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Depth { get; set; }
    public int? Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
    public Job Job { get; set; }
}