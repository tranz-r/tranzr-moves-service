using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class CustomerJob : IAuditable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public Guid JobId { get; set; }
    public Job Job { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}