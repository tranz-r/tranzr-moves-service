namespace TranzrMoves.Domain.Interfaces
{
    public interface IAuditable
    {
        public DateTimeOffset CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset ModifiedAt { get; set; } 
        public string ModifiedBy { get; set; }
    }
}