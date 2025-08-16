using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Api.Dtos;
using TranzrMoves.Api.Entities;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class AuthController(ILogger<AuthController> logger, IConfiguration configuration, Supabase.Client client, IMediator mediator) : ApiControllerBase
{
    [HttpPost("role")]
    public async Task<ActionResult<UserRoleResponse>> CreateUserRoleAsync([FromBody] UserRoleRequest userRoleRequest)
    {
        var modeledResponse = await client.From<UserRole>()
            .Where(x => x.UserId == userRoleRequest.UserId).Get();

        var userRoles = modeledResponse.Models;

        if (userRoles.Any(x => x.Role == userRoleRequest.Role))
        {
            logger.LogWarning("User {userId} already exists", userRoleRequest.UserId);
            return userRoles.Select(x => new UserRoleResponse
            {
                Id = x.Id,
                UserId = x.UserId,
                Role = userRoleRequest.Role
            }).First(x => x.Role == userRoleRequest.Role);
        }
        
        logger.LogInformation("Adding role of {userRole} to  user {userRoleRequest.UserId}",  userRoleRequest.Role,  userRoleRequest.UserId);
        var dbResponse = await client.From<UserRole>().Insert(new UserRole
        {
            UserId = userRoleRequest.UserId,
            Role = userRoleRequest.Role,
        });
        
        var userRoleInDb = dbResponse.Model;
        
        return Ok(new  UserRoleResponse
        {
            Id = userRoleInDb?.Id,
            UserId = userRoleInDb?.UserId,
            Role = userRoleInDb?.Role
        });
    }
    
    [HttpPost("role-permissions")]
    public async Task<ActionResult<List<UserRoleResponse>>> GetRolePermissionsAsync([FromQuery] string roleName)
    {
        var modeledResponse = await client.From<RolePermissions>()
            .Where(x => x.Role == roleName).Get();
        
        var rolePermissions = modeledResponse.Models;
        
        return Ok(rolePermissions.Select(x => new RolePermissionsResponse
        {
            Id = x.Id,
            Role = x.Role,
            Permission = x.Permission
        }).ToList());
    }
    
    // [HttpPost("password-reset")]
    // public async Task ResetUserPasswordAsync([FromQuery] string email)
    // {
    //     var resp = await client.AdminAuth(configuration["SUPER_BASE_SERVICE_ROLE_KEY"])
    //         .GenerateLink(new GenerateLinkOptions(GenerateLinkOptions.LinkType.Recovery, email));
    // }
}