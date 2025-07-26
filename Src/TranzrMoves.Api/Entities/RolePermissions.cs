using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace TranzrMoves.Api.Entities;

[Table("role_permissions")]
public class RolePermissions : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    [Column("role")]
    public string Role { get; set; }
    [Column("permission")]
    public string Permission { get; set; }
}