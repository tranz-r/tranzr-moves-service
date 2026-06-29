using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.BusinessAccount.Activate;
using TranzrMoves.Application.Features.BusinessAccount.Get;
using TranzrMoves.Application.Features.BusinessAccount.Register;
using TranzrMoves.Application.Features.BusinessAccount.Suspend;
using TranzrMoves.Application.Features.BusinessAccount.Update;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.UnitTests.Application;

public sealed class RegisterBusinessAccountCommandHandlerTests
{
    private readonly ICurrentBusinessUserContext _currentUserContext = Substitute.For<ICurrentBusinessUserContext>();
    private readonly IUserV2Repository _userRepository = Substitute.For<IUserV2Repository>();
    private readonly IBusinessUserRepository _businessUserRepository = Substitute.For<IBusinessUserRepository>();
    private readonly IBusinessAccountRepository _businessAccountRepository = Substitute.For<IBusinessAccountRepository>();
    private readonly ITurnstileService _turnstileService = Substitute.For<ITurnstileService>();
    private readonly ISupabaseAuthAdminService _supabaseAuthAdminService = Substitute.For<ISupabaseAuthAdminService>();
    private readonly BusinessAccountMapper _mapper = new();
    private readonly RegisterBusinessAccountCommandHandler _handler;

    public RegisterBusinessAccountCommandHandlerTests()
    {
        _turnstileService.ValidateTokenAsync(Arg.Any<string>(), remoteIp: null, Arg.Any<CancellationToken>())
            .Returns(true);

        _supabaseAuthAdminService
            .UpdateUserNameAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success);

        _handler = new RegisterBusinessAccountCommandHandler(
            _currentUserContext,
            _userRepository,
            _businessUserRepository,
            _businessAccountRepository,
            _turnstileService,
            _supabaseAuthAdminService,
            _mapper,
            NullLogger<RegisterBusinessAccountCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ReturnsValidationError_WhenTurnstileValidationFails()
    {
        _turnstileService.ValidateTokenAsync(Arg.Any<string>(), remoteIp: null, Arg.Any<CancellationToken>())
            .Returns(Error.Validation("Turnstile.Invalid", "Invalid token"));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("BusinessAccount.TurnstileValidation");
        await _userRepository.DidNotReceive().GetUserBySupabaseIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsValidationError_WhenOwnerEmailDoesNotMatchJwtEmail()
    {
        _currentUserContext.SupabaseId.Returns(Guid.NewGuid());
        _currentUserContext.Email.Returns("jwt@example.com");

        var command = CreateCommand(ownerEmail: "owner@example.com");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("BusinessAccount.OwnerEmailMismatch");
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenUserAlreadyHasBusinessMembership()
    {
        var supabaseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentUserContext.SupabaseId.Returns(supabaseId);
        _currentUserContext.Email.Returns("owner@example.com");
        _userRepository.GetUserBySupabaseIdAsync(supabaseId, Arg.Any<CancellationToken>())
            .Returns(new UserV2 { Id = userId, Email = "owner@example.com", SupabaseId = supabaseId });
        _businessUserRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new BusinessUser { Id = Guid.NewGuid(), UserId = userId, BusinessAccountId = Guid.NewGuid() });

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("BusinessAccount.AlreadyRegistered");
    }

    [Fact]
    public async Task Handle_CreatesBusinessAccount_WhenRequestIsValid()
    {
        var supabaseId = Guid.NewGuid();
        _currentUserContext.SupabaseId.Returns(supabaseId);
        _currentUserContext.Email.Returns("owner@example.com");
        _userRepository.GetUserBySupabaseIdAsync(supabaseId, Arg.Any<CancellationToken>())
            .Returns((UserV2?)null);
        _userRepository.GetUserByEmailAsync("owner@example.com", Arg.Any<CancellationToken>())
            .Returns((UserV2?)null);

        var userId = Guid.NewGuid();
        var businessAccountId = Guid.NewGuid();
        var businessUserId = Guid.NewGuid();
        _businessAccountRepository.RegisterAsync(
                Arg.Any<UserV2>(),
                Arg.Any<Domain.Entities.BusinessAccount>(),
                Arg.Any<BusinessUser>(),
                Arg.Any<CancellationToken>())
            .Returns(new RegisterBusinessAccountResult(
                userId,
                businessAccountId,
                businessUserId,
                BusinessUserRole.Owner,
                BusinessUserStatus.Active));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.BusinessAccountId.Should().Be(businessAccountId);
        result.Value.Role.Should().Be(BusinessUserRole.Owner);
        result.Value.Status.Should().Be(BusinessUserStatus.Active);

        await _supabaseAuthAdminService.Received(1)
            .UpdateUserNameAsync(supabaseId, "Jane", "Owner", Arg.Any<CancellationToken>());
    }

    private static RegisterBusinessAccountCommand CreateCommand(string ownerEmail = "owner@example.com") =>
        new(
            BusinessName: "Acme Removals",
            TradingName: "Acme",
            BusinessEmail: "billing@acme.example",
            BusinessPhone: "+441234567890",
            BillingAddress: new AddressDto
            {
                Line1 = "1 High Street",
                PostCode = "SW1A 1AA",
            },
            CompanyRegistrationNumber: "12345678",
            VatNumber: "GB123456789",
            Owner: new BusinessOwnerSignupDto
            {
                FirstName = "Jane",
                LastName = "Owner",
                Email = ownerEmail,
                PhoneNumber = "+449876543210",
            },
            TurnstileToken: "test-turnstile-token");
}

public sealed class GetBusinessAccountQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsForbidden_WhenRequestedAccountDoesNotMatchMembership()
    {
        var businessAccountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var currentUserContext = Substitute.For<ICurrentBusinessUserContext>();
        currentUserContext.GetBusinessUserAsync(Arg.Any<CancellationToken>())
            .Returns(new BusinessUser
            {
                Id = Guid.NewGuid(),
                BusinessAccountId = businessAccountId,
                UserId = Guid.NewGuid(),
                Role = BusinessUserRole.Owner,
                Status = BusinessUserStatus.Active,
            });

        var handler = new GetBusinessAccountQueryHandler(
            currentUserContext,
            Substitute.For<IBusinessAccountRepository>(),
            new BusinessAccountMapper());

        var result = await handler.Handle(new GetBusinessAccountQuery(otherAccountId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("BusinessAccount.Forbidden");
    }
}

public sealed class UpdateBusinessAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesBusinessAccount_ForCurrentOwner()
    {
        var businessAccountId = Guid.NewGuid();
        var currentUserContext = Substitute.For<ICurrentBusinessUserContext>();
        currentUserContext.BusinessAccountId.Returns(businessAccountId);

        var existingAccount = new Domain.Entities.BusinessAccount
        {
            Id = businessAccountId,
            BusinessName = "Old Name",
            BusinessEmail = "old@acme.example",
            BusinessPhone = "+441111111111",
            Status = BusinessAccountStatus.Active,
            BillingAddress = new BillingAddress
            {
                Line1 = "Old Street",
                PostCode = "AB1 2CD",
            },
        };

        var repository = Substitute.For<IBusinessAccountRepository>();
        repository.GetByIdAsync(businessAccountId, Arg.Any<CancellationToken>())
            .Returns(existingAccount);
        repository.UpdateAsync(Arg.Any<Domain.Entities.BusinessAccount>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Domain.Entities.BusinessAccount>());

        var handler = new UpdateBusinessAccountCommandHandler(
            currentUserContext,
            repository,
            new BusinessAccountMapper());

        var command = new UpdateBusinessAccountCommand(
            Id: businessAccountId,
            BusinessName: "New Name",
            TradingName: "New Trading",
            BusinessEmail: "new@acme.example",
            BusinessPhone: "+442222222222",
            BillingAddress: new AddressDto
            {
                Line1 = "New Street",
                PostCode = "EF3 4GH",
            },
            CompanyRegistrationNumber: "87654321",
            VatNumber: "GB987654321");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.BusinessName.Should().Be("New Name");
        result.Value.BusinessEmail.Should().Be("new@acme.example");
        result.Value.BillingAddress.Line1.Should().Be("New Street");
    }
}

public sealed class SuspendActivateBusinessAccountCommandHandlerTests
{
    [Fact]
    public async Task Suspend_SetsAccountStatusToSuspended()
    {
        var accountId = Guid.NewGuid();
        var repository = Substitute.For<IBusinessAccountRepository>();
        repository.SuspendAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(new Domain.Entities.BusinessAccount
            {
                Id = accountId,
                BusinessName = "Acme",
                BusinessEmail = "billing@acme.example",
                BusinessPhone = "+441234567890",
                Status = BusinessAccountStatus.Suspended,
                BillingAddress = new BillingAddress { Line1 = "1 High Street", PostCode = "SW1A 1AA" },
            });

        var handler = new SuspendBusinessAccountCommandHandler(repository, new BusinessAccountMapper());
        var result = await handler.Handle(new SuspendBusinessAccountCommand(accountId), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(BusinessAccountStatus.Suspended);
    }

    [Fact]
    public async Task Activate_SetsAccountStatusToActive()
    {
        var accountId = Guid.NewGuid();
        var repository = Substitute.For<IBusinessAccountRepository>();
        repository.ActivateAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(new Domain.Entities.BusinessAccount
            {
                Id = accountId,
                BusinessName = "Acme",
                BusinessEmail = "billing@acme.example",
                BusinessPhone = "+441234567890",
                Status = BusinessAccountStatus.Active,
                BillingAddress = new BillingAddress { Line1 = "1 High Street", PostCode = "SW1A 1AA" },
            });

        var handler = new ActivateBusinessAccountCommandHandler(repository, new BusinessAccountMapper());
        var result = await handler.Handle(new ActivateBusinessAccountCommand(accountId), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(BusinessAccountStatus.Active);
    }
}
