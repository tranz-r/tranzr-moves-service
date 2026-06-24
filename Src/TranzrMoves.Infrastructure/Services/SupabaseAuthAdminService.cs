using ErrorOr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services;

public sealed class SupabaseAuthAdminService(
    IConfiguration configuration,
    ILogger<SupabaseAuthAdminService> logger) : ISupabaseAuthAdminService
{
    public async Task<ErrorOr<SupabaseAuthUser>> CreateUserAsync(
        SupabaseAuthUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        var url = configuration["SUPABASE_URL"];
        var serviceRoleKey = configuration["SUPABASE_SERVICE_ROLE_KEY"];

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(serviceRoleKey))
        {
            logger.LogCritical("Missing Supabase configuration for admin user provisioning");
            return Error.Failure(
                code: "Supabase.Configuration",
                description: "Supabase auth is not configured.");
        }

        var metadata = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            metadata["first_name"] = request.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            metadata["last_name"] = request.LastName;
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            metadata["phone_number"] = request.PhoneNumber;
        }

        var attributes = new AdminUserAttributes
        {
            Email = request.Email,
            EmailConfirm = true,
            Phone = request.PhoneNumber,
        };

        if (metadata.Count > 0)
        {
            attributes.UserMetadata = metadata;
        }

        var authUrl = url.TrimEnd('/') + "/auth/v1";
        var adminClient = new AdminClient(
            serviceRoleKey,
            new ClientOptions
            {
                Url = authUrl,
            });

        try
        {
            var user = await adminClient.CreateUser(attributes);
            if (user is null || string.IsNullOrWhiteSpace(user.Id) || !Guid.TryParse(user.Id, out var supabaseId))
            {
                logger.LogError("Supabase admin create user returned an invalid response for {Email}", request.Email);
                return Error.Failure(
                    code: "Supabase.CreateUser",
                    description: "Failed to create the auth account.");
            }

            return new SupabaseAuthUser(supabaseId, request.Email);
        }
        catch (GotrueException ex) when (IsDuplicateUser(ex))
        {
            logger.LogWarning(ex, "Supabase auth account already exists for {Email}", request.Email);
            return Error.Conflict(
                code: "Auth.EmailAlreadyRegistered",
                description: "An account with this email already exists.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Supabase auth account for {Email}", request.Email);
            return Error.Failure(
                code: "Supabase.CreateUser",
                description: "Failed to create the auth account.");
        }
    }

    private static bool IsDuplicateUser(GotrueException exception) =>
        exception.Message.Contains("already", StringComparison.OrdinalIgnoreCase)
        || exception.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
        || exception.Message.Contains("exists", StringComparison.OrdinalIgnoreCase);
}
