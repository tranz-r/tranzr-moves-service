using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessAccount.Register;

public sealed record RegisterBusinessAccountCommand(
    string BusinessName,
    string? TradingName,
    string BusinessEmail,
    string BusinessPhone,
    AddressDto BillingAddress,
    string? CompanyRegistrationNumber,
    string? VatNumber,
    BusinessOwnerSignupDto Owner,
    string TurnstileToken) : IRequest<ErrorOr<RegisterBusinessAccountResponse>>;

public sealed class RegisterBusinessAccountCommandValidator : AbstractValidator<RegisterBusinessAccountCommand>
{
    public RegisterBusinessAccountCommandValidator()
    {
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TradingName).MaximumLength(200);
        RuleFor(x => x.BusinessEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.BusinessPhone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CompanyRegistrationNumber).MaximumLength(50);
        RuleFor(x => x.VatNumber).MaximumLength(50);
        RuleFor(x => x.BillingAddress).NotNull();
        RuleFor(x => x.BillingAddress.Line1).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BillingAddress.PostCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Owner).NotNull();
        RuleFor(x => x.Owner.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Owner.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Owner.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Owner.PhoneNumber).NotEmpty().MaximumLength(32);
        RuleFor(x => x.TurnstileToken).NotEmpty();
    }
}

public sealed class RegisterBusinessAccountCommandHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IUserV2Repository userV2Repository,
    IBusinessUserRepository businessUserRepository,
    IBusinessAccountRepository businessAccountRepository,
    ITurnstileService turnstileService,
    ISupabaseAuthAdminService supabaseAuthAdminService,
    BusinessAccountMapper mapper,
    ILogger<RegisterBusinessAccountCommandHandler> logger)
    : IRequestHandler<RegisterBusinessAccountCommand, ErrorOr<RegisterBusinessAccountResponse>>
{
    public async ValueTask<ErrorOr<RegisterBusinessAccountResponse>> Handle(
        RegisterBusinessAccountCommand command,
        CancellationToken cancellationToken)
    {
        var turnstileValidation = await turnstileService.ValidateTokenAsync(
            command.TurnstileToken,
            cancellationToken: cancellationToken);
        if (turnstileValidation.IsError)
        {
            logger.LogWarning(
                "Turnstile validation failed for business account registration for {Email}",
                command.Owner.Email);
            return Error.Validation(
                code: "BusinessAccount.TurnstileValidation",
                description: "Security verification failed. Please try again.");
        }

        var supabaseId = currentBusinessUserContext.SupabaseId;
        var jwtEmail = currentBusinessUserContext.Email;

        if (supabaseId is null || string.IsNullOrWhiteSpace(jwtEmail))
        {
            return Error.Unauthorized(
                code: "BusinessAccount.Unauthorized",
                description: "A valid authenticated session is required.");
        }

        var ownerEmail = command.Owner.Email.Trim();
        if (!string.Equals(ownerEmail, jwtEmail.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation(
                code: "BusinessAccount.OwnerEmailMismatch",
                description: "Owner email must match the authenticated user's email.");
        }

        var existingBySupabase = await userV2Repository.GetUserBySupabaseIdAsync(supabaseId.Value, cancellationToken);
        if (existingBySupabase is not null)
        {
            var existingMembership = await businessUserRepository.GetByUserIdAsync(existingBySupabase.Id, cancellationToken);
            if (existingMembership is not null)
            {
                return Error.Conflict(
                    code: "BusinessAccount.AlreadyRegistered",
                    description: "This user already belongs to a business account.");
            }

            return Error.Conflict(
                code: "BusinessAccount.UserAlreadyExists",
                description: "An app user already exists for this authenticated account.");
        }

        var existingByEmail = await userV2Repository.GetUserByEmailAsync(ownerEmail, cancellationToken);
        if (existingByEmail is not null)
        {
            return Error.Conflict(
                code: "BusinessAccount.EmailAlreadyRegistered",
                description: "An account with this email already exists.");
        }

        var userId = Guid.NewGuid();
        var businessAccountId = Guid.NewGuid();
        var businessUserId = Guid.NewGuid();

        var user = new UserV2
        {
            Id = userId,
            Email = ownerEmail,
            FirstName = command.Owner.FirstName.Trim(),
            LastName = command.Owner.LastName.Trim(),
            PhoneNumber = command.Owner.PhoneNumber.Trim(),
            SupabaseId = supabaseId.Value,
        };

        var businessAccount = new Domain.Entities.BusinessAccount
        {
            Id = businessAccountId,
            BusinessName = command.BusinessName.Trim(),
            TradingName = command.TradingName?.Trim(),
            BusinessEmail = command.BusinessEmail.Trim(),
            BusinessPhone = command.BusinessPhone.Trim(),
            CompanyRegistrationNumber = command.CompanyRegistrationNumber?.Trim(),
            VatNumber = command.VatNumber?.Trim(),
            Status = BusinessAccountStatus.Active,
            BillingAddress = mapper.ToBillingAddress(command.BillingAddress),
        };

        var businessUser = new Domain.Entities.BusinessUser
        {
            Id = businessUserId,
            BusinessAccountId = businessAccountId,
            UserId = userId,
            Role = BusinessUserRole.Owner,
            Status = BusinessUserStatus.Active,
        };

        var result = await businessAccountRepository.RegisterAsync(
            user,
            businessAccount,
            businessUser,
            cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        var registration = result.Value;
        logger.LogInformation(
            "Registered business account {BusinessAccountId} with owner {BusinessUserId}",
            registration.BusinessAccountId,
            registration.BusinessUserId);

        // Capture the owner's name in Supabase user_metadata so GoTrue email templates can render
        // it uniformly (same first_name/last_name keys as invited users). The owner's auth account
        // was created passwordlessly (OTP) and may not carry the name yet. Best-effort: a failure
        // here must not fail the already-committed registration.
        var metadataSync = await supabaseAuthAdminService.UpdateUserNameAsync(
            supabaseId.Value,
            user.FirstName,
            user.LastName,
            cancellationToken);
        if (metadataSync.IsError)
        {
            logger.LogWarning(
                "Registered business account {BusinessAccountId} but failed to sync owner name to Supabase metadata for {SupabaseId}: {Error}",
                registration.BusinessAccountId,
                supabaseId.Value,
                metadataSync.FirstError.Description);
        }

        return new RegisterBusinessAccountResponse
        {
            BusinessAccountId = registration.BusinessAccountId,
            BusinessUserId = registration.BusinessUserId,
            UserId = registration.UserId,
            Role = registration.Role,
            Status = registration.Status,
        };
    }
}
