namespace TranzrMoves.Infrastructure.Authentication;

public static class AuthorizationPolicies
{
    public const string BusinessUser = nameof(BusinessUser);
    public const string BusinessOwner = nameof(BusinessOwner);
    public const string BusinessAdmin = nameof(BusinessAdmin);
    public const string BusinessFinance = nameof(BusinessFinance);
}

public static class SupabaseClaimTypes
{
    public const string Email = "email";
}
