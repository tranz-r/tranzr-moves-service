using System.Linq;
using System.Text;
using System.Text.Json;
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
    private int _serviceKeyChecked;

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

        var keyCheck = EnsureServiceKeyUsable();
        if (keyCheck.IsError)
        {
            return keyCheck.Errors;
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

    public Task<ErrorOr<Success>> InviteUserByEmailAsync(
        SupabaseInviteUserRequest request,
        CancellationToken cancellationToken)
        => SendInviteAsync(request, isResend: false);

    public Task<ErrorOr<Success>> ResendInvitationAsync(
        SupabaseInviteUserRequest request,
        CancellationToken cancellationToken)
        => SendInviteAsync(request, isResend: true);

    private async Task<ErrorOr<Success>> SendInviteAsync(
        SupabaseInviteUserRequest request,
        bool isResend)
    {
        if (!_options.IsConfigured)
        {
            logger.LogCritical("Missing Supabase configuration for admin user invitation");
            return Error.Failure(
                code: "Supabase.Configuration",
                description: "Supabase auth is not configured.");
        }

        var keyCheck = EnsureServiceKeyUsable();
        if (keyCheck.IsError)
        {
            return keyCheck.Errors;
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

        // Supabase's admin /invite endpoint returns 422 for an email that already has an auth
        // account and does NOT re-send the email. For a resend we therefore delete the existing
        // (still-unconfirmed) invited account first, so the invite below issues a fresh link and
        // Supabase mails it again.
        if (isResend)
        {
            var prepared = await RemoveUnconfirmedInviteeAsync(adminClient, request.Email);
            if (prepared.IsError)
            {
                return prepared.Errors;
            }
        }

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

    /// <summary>
    /// Removes the existing invited (still-unconfirmed) Supabase auth account for <paramref name="email"/>
    /// so a fresh invite link can be issued and re-emailed. If the account has already confirmed its
    /// email (i.e. the invitation was accepted) it is left intact and a conflict is returned.
    /// </summary>
    private async Task<ErrorOr<Success>> RemoveUnconfirmedInviteeAsync(AdminClient adminClient, string email)
    {
        try
        {
            var list = await adminClient.ListUsers(filter: email, perPage: 200);
            var existing = list?.Users.FirstOrDefault(
                u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

            if (existing?.Id is null)
            {
                // Nothing to remove; the invite below will create a fresh account.
                return Result.Success;
            }

            if (existing.EmailConfirmedAt is not null || existing.ConfirmedAt is not null)
            {
                // The invitee has already accepted — never delete a live account.
                return Error.Conflict(
                    code: "BusinessUser.AlreadyAccepted",
                    description: "This invitation has already been accepted.");
            }

            await adminClient.DeleteUser(existing.Id);
            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset the existing invited auth account for {Email}", email);
            return Error.Failure(
                code: "Supabase.ResendInvite",
                description: "Failed to prepare the invitation for resend.");
        }
    }

    public async Task<ErrorOr<Success>> UpdateUserNameAsync(
        Guid supabaseId,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            logger.LogCritical("Missing Supabase configuration for admin user metadata update");
            return Error.Failure(
                code: "Supabase.Configuration",
                description: "Supabase auth is not configured.");
        }

        var keyCheck = EnsureServiceKeyUsable();
        if (keyCheck.IsError)
        {
            return keyCheck.Errors;
        }

        // Uniform metadata keys shared with the invite/create flows so GoTrue email templates
        // can render {{ .Data.first_name }} / {{ .Data.last_name }} regardless of how the user
        // was provisioned.
        var metadata = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(firstName))
        {
            metadata["first_name"] = firstName;
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            metadata["last_name"] = lastName;
        }

        if (metadata.Count == 0)
        {
            return Result.Success;
        }

        var adminClient = CreateAdminClient();

        try
        {
            // GoTrue merges the supplied user_metadata keys, leaving any others intact.
            var user = await adminClient.UpdateUserById(
                supabaseId.ToString(),
                new AdminUserAttributes { UserMetadata = metadata });

            if (user is null)
            {
                logger.LogError("Supabase admin update user returned null for {SupabaseId}", supabaseId);
                return Error.Failure(
                    code: "Supabase.UpdateUser",
                    description: "Failed to update the auth account metadata.");
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update Supabase user metadata for {SupabaseId}", supabaseId);
            return Error.Failure(
                code: "Supabase.UpdateUser",
                description: "Failed to update the auth account metadata.");
        }
    }

    private AdminClient CreateAdminClient() =>
        new(_options.SecretKey!, new ClientOptions
        {
            Url = _options.AuthUrl,
            // The Gotrue AdminClient only sets the Authorization bearer token; Supabase's API
            // gateway also requires an "apikey" header on every request ("No API key found in
            // request" otherwise). Supply the same elevated key via the constructor headers so it
            // is merged into every admin call (create user, invite, resend, etc.). Works with both
            // the new secret key (sb_secret_…) and the legacy service_role JWT.
            Headers = new Dictionary<string, string>
            {
                ["apikey"] = _options.SecretKey!,
            },
        });

    /// <summary>
    /// Guards against an obviously wrong key before any admin call is attempted. Supabase admin
    /// operations (invite/create user) require an <em>elevated</em> key: either the new secret key
    /// (<c>sb_secret_…</c>) or the legacy <c>service_role</c>/<c>supabase_admin</c> JWT. A low-privilege
    /// key (publishable <c>sb_publishable_…</c> or an <c>anon</c> JWT) is rejected here with a clear
    /// message instead of surfacing a confusing HTTP 401 "Unauthorized" from Supabase.
    /// </summary>
    private ErrorOr<Success> EnsureServiceKeyUsable()
    {
        var inspection = ServiceKeyInspection.Read(_options.SecretKey!);
        var logOnce = Interlocked.Exchange(ref _serviceKeyChecked, 1) == 0;

        if (inspection.IsLowPrivilege)
        {
            if (logOnce)
            {
                logger.LogCritical(
                    "SUPABASE_SECRET_KEY looks like a low-privilege key ({Kind}). Supabase admin calls " +
                    "(invite/create user) require an elevated key: the new secret key (sb_secret_…) or the legacy " +
                    "service_role JWT. Update SUPABASE_SECRET_KEY from Project Settings → API Keys.",
                    inspection.Kind);
            }

            return Error.Failure(
                code: "Supabase.ServiceKey",
                description: "Supabase key is not an elevated (admin) key.");
        }

        return Result.Success;
    }

    private readonly record struct ServiceKeyInspection(bool IsLowPrivilege, string Kind)
    {
        public static ServiceKeyInspection Read(string key)
        {
            // New-format API keys are self-describing by prefix.
            if (key.StartsWith("sb_secret_", StringComparison.Ordinal))
            {
                return new ServiceKeyInspection(false, "secret key");
            }

            if (key.StartsWith("sb_publishable_", StringComparison.Ordinal))
            {
                return new ServiceKeyInspection(true, "publishable key (sb_publishable_…)");
            }

            // Legacy keys are JWTs whose privilege is carried in the "role" claim.
            var parts = key.Split('.');
            if (parts.Length == 3)
            {
                var role = ReadRoleClaim(parts[1]);
                if (string.Equals(role, "anon", StringComparison.OrdinalIgnoreCase))
                {
                    return new ServiceKeyInspection(true, "anon JWT");
                }

                // service_role / supabase_admin (or any other role) is treated as elevated.
                return new ServiceKeyInspection(false, $"{role ?? "unknown"} JWT");
            }

            // Unrecognised format — don't block; let Supabase be the authority.
            return new ServiceKeyInspection(false, "unrecognised key");
        }

        private static string? ReadRoleClaim(string payloadSegment)
        {
            try
            {
                using var doc = JsonDocument.Parse(DecodeBase64Url(payloadSegment));
                return doc.RootElement.TryGetProperty("role", out var roleEl) ? roleEl.GetString() : null;
            }
            catch
            {
                return null;
            }
        }

        private static string DecodeBase64Url(string segment)
        {
            var normalized = segment.Replace('-', '+').Replace('_', '/');
            switch (normalized.Length % 4)
            {
                case 2: normalized += "=="; break;
                case 3: normalized += "="; break;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
        }
    }

    private static bool IsDuplicateUser(GotrueException exception) =>
        exception.Message.Contains("already", StringComparison.OrdinalIgnoreCase)
        || exception.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
        || exception.Message.Contains("exists", StringComparison.OrdinalIgnoreCase);
}
