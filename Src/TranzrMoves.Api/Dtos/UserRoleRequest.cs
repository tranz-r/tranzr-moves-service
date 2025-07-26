namespace TranzrMoves.Api.Dtos;

public class UserRoleRequest
{
    public Guid UserId { get; set; }
    public required string Role { get; set; }
}