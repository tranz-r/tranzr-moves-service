using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Authentication;

public sealed class CurrentBusinessUserContext(
    IHttpContextAccessor httpContextAccessor,
    IBusinessUserRepository businessUserRepository,
    ITenantProvider tenantProvider) : ICurrentBusinessUserContext
{
    private BusinessUser? _cachedBusinessUser;

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid? SupabaseId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(SupabaseClaimTypes.Email)
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public Guid? UserId => _cachedBusinessUser?.UserId;

    public Guid? BusinessAccountId => _cachedBusinessUser?.BusinessAccountId;

    public Guid? BusinessUserId => _cachedBusinessUser?.Id;

    public BusinessUserRole? Role => _cachedBusinessUser?.Role;

    public BusinessUserStatus? Status => _cachedBusinessUser?.Status;

    public async Task<BusinessUser?> GetBusinessUserAsync(CancellationToken cancellationToken)
    {
        if (_cachedBusinessUser is not null)
        {
            return _cachedBusinessUser;
        }

        var supabaseId = SupabaseId;
        if (supabaseId is null)
        {
            return null;
        }

        _cachedBusinessUser = await businessUserRepository.GetBySupabaseIdAsync(supabaseId.Value, cancellationToken);
        if (_cachedBusinessUser is not null)
        {
            tenantProvider.SetBusinessAccount(_cachedBusinessUser.BusinessAccountId);
        }

        return _cachedBusinessUser;
    }
}
