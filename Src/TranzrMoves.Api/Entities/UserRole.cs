using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace TranzrMoves.Api.Entities;

[Table("user_roles")]
public class UserRole : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    [Column("user_id")]
    public Guid UserId { get; set; }
    [Column("role")]
    public string? Role { get; set; }
}