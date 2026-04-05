namespace TranzrMoves.Domain.Interfaces
{
    public interface IAuditable
    {
        public Instant CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public Instant ModifiedAt { get; set; } 
        public string ModifiedBy { get; set; }
    }
}