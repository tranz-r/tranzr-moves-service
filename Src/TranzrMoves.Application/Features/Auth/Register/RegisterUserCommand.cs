using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Auth.Register;

public sealed record RegisterUserCommand(
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber) : IRequest<ErrorOr<RegisterUserResponse>>;

public sealed record RegisterUserResponse(
    Guid UserId,
    Guid SupabaseId,
    string Email);

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.FirstName)
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .MaximumLength(100);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(32);
    }
}

public sealed class RegisterUserCommandHandler(
    ISupabaseAuthAdminService supabaseAuthAdminService,
    IUserV2Repository userV2Repository,
    ILogger<RegisterUserCommandHandler> logger)
    : IRequestHandler<RegisterUserCommand, ErrorOr<RegisterUserResponse>>
{
    public async ValueTask<ErrorOr<RegisterUserResponse>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim();

        var existingUser = await userV2Repository.GetUserByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            logger.LogWarning("Registration rejected because app user already exists for {Email}", normalizedEmail);
            return Error.Conflict(
                code: "Auth.EmailAlreadyRegistered",
                description: "An account with this email already exists.");
        }

        var authUserResult = await supabaseAuthAdminService.CreateUserAsync(
            new SupabaseAuthUserCreateRequest(
                normalizedEmail,
                command.FirstName?.Trim(),
                command.LastName?.Trim(),
                command.PhoneNumber?.Trim()),
            cancellationToken);

        if (authUserResult.IsError)
        {
            return authUserResult.Errors;
        }

        var authUser = authUserResult.Value;
        var appUserResult = await userV2Repository.AddUserAsync(
            new UserV2
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                FirstName = command.FirstName?.Trim(),
                LastName = command.LastName?.Trim(),
                PhoneNumber = command.PhoneNumber?.Trim(),
                SupabaseId = authUser.Id,
            },
            cancellationToken);

        if (appUserResult.IsError)
        {
            logger.LogError(
                "Created Supabase auth user {SupabaseId} but failed to persist app user for {Email}",
                authUser.Id,
                normalizedEmail);
            return appUserResult.Errors;
        }

        var appUser = appUserResult.Value;
        logger.LogInformation(
            "Registered app user {UserId} linked to Supabase auth user {SupabaseId}",
            appUser.Id,
            authUser.Id);

        return new RegisterUserResponse(appUser.Id, authUser.Id, normalizedEmail);
    }
}
