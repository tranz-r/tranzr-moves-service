namespace TranzrMoves.Application.Contracts;

public class InventoryItemDto
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Depth { get; set; }
    public int? Quantity { get; set; }
}
