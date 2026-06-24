using FluentValidation;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.BusinessAccount.Update;

public sealed record UpdateBusinessAccountCommand(
    Guid Id,
    string BusinessName,
    string? TradingName,
    string BusinessEmail,
    string BusinessPhone,
    AddressDto BillingAddress,
    string? CompanyRegistrationNumber,
    string? VatNumber) : IRequest<ErrorOr<BusinessAccountDto>>;

public sealed class UpdateBusinessAccountCommandValidator : AbstractValidator<UpdateBusinessAccountCommand>
{
    public UpdateBusinessAccountCommandValidator()
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
    }
}

public sealed class UpdateBusinessAccountCommandHandler(
    ICurrentBusinessUserContext currentBusinessUserContext,
    IBusinessAccountRepository businessAccountRepository,
    BusinessAccountMapper mapper)
    : IRequestHandler<UpdateBusinessAccountCommand, ErrorOr<BusinessAccountDto>>
{
    public async ValueTask<ErrorOr<BusinessAccountDto>> Handle(
        UpdateBusinessAccountCommand command,
        CancellationToken cancellationToken)
    {
        var businessAccountId = currentBusinessUserContext.BusinessAccountId;
        if (businessAccountId is null || businessAccountId.Value != command.Id)
        {
            return Error.Forbidden(
                code: "BusinessAccount.Forbidden",
                description: "Only business account owners can update account details.");
        }

        var account = await businessAccountRepository.GetByIdAsync(command.Id, cancellationToken);
        if (account is null)
        {
            return Error.NotFound(
                code: "BusinessAccount.NotFound",
                description: "Business account not found.");
        }

        account.BusinessName = command.BusinessName.Trim();
        account.TradingName = command.TradingName?.Trim();
        account.BusinessEmail = command.BusinessEmail.Trim();
        account.BusinessPhone = command.BusinessPhone.Trim();
        account.CompanyRegistrationNumber = command.CompanyRegistrationNumber?.Trim();
        account.VatNumber = command.VatNumber?.Trim();
        account.BillingAddress = mapper.ToBillingAddress(command.BillingAddress);

        var updateResult = await businessAccountRepository.UpdateAsync(account, cancellationToken);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        return mapper.ToBusinessAccountDto(updateResult.Value);
    }
}
