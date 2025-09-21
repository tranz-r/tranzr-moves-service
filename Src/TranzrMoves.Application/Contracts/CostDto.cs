namespace TranzrMoves.Application.Contracts;

public class CostDto
{
    public Guid JobId { get; set; }
    public long BaseVan { get; set; }
    public double Distance { get; set; }
    public long Floor { get; set; }
    public long ElevatorAdjustment  { get; set; }
    public long Driver  { get; set; }
    public double TierAdjustment  { get; set; }
    public double Total  { get; set; }
}