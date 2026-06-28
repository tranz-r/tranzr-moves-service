using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services;

public sealed class SupabaseAuthAdminService(
    IOptions<SupabaseAuthOptions> options,
    ILogger<SupabaseAuthAdminService> logger) : ISupabaseAuthAdminService
{
    private readonly SupabaseAuthOptions _options = options.Value;

    public async Task<ErrorOr<SupabaseAuthUser>> CreateUserAsync(
        SupabaseAuthUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
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

        var adminClient = CreateAdminClient();

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

    public async Task<ErrorOr<Success>> InviteUserByEmailAsync(
        SupabaseInviteUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            logger.LogCritical("Missing Supabase configuration for admin user invitation");
            return Error.Failure(
                code: "Supabase.Configuration",
                description: "Supabase auth is not configured.");
        }

        var metadata = new Dictionary<string, object>
        {
            ["role"] = request.Role,
            ["business_account_id"] = request.BusinessAccountId.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            metadata["first_name"] = request.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            metadata["last_name"] = request.LastName;
        }

        var inviteOptions = new InviteUserByEmailOptions
        {
            Data = metadata,
            RedirectTo = _options.InviteRedirectUrl,
        };

        var adminClient = CreateAdminClient();

        try
        {
            var invited = await adminClient.InviteUserByEmail(request.Email, inviteOptions);
            if (!invited)
            {
                logger.LogError("Supabase admin invite returned false for {Email}", request.Email);
                return Error.Failure(
                    code: "Supabase.InviteUser",
                    description: "Failed to invite the user.");
            }

            return Result.Success;
        }
        catch (GotrueException ex) when (IsDuplicateUser(ex))
        {
            logger.LogWarning(ex, "Supabase auth account already exists for invited {Email}", request.Email);
            return Error.Conflict(
                code: "BusinessUser.AuthAccountExists",
                description: "An account with this email already exists.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to invite Supabase auth account for {Email}", request.Email);
            return Error.Failure(
                code: "Supabase.InviteUser",
                description: "Failed to invite the user.");
        }
    }

    private AdminClient CreateAdminClient() =>
        new(_options.ServiceRoleKey!, new ClientOptions { Url = _options.AuthUrl });

    private static bool IsDuplicateUser(GotrueException exception) =>
        exception.Message.Contains("already", StringComparison.OrdinalIgnoreCase)
        || exception.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
        || exception.Message.Contains("exists", StringComparison.OrdinalIgnoreCase);
}
