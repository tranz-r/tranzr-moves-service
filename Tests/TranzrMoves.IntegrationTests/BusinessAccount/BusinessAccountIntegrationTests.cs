using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.BusinessAccount.Activate;
using TranzrMoves.Application.Features.BusinessAccount.Register;
using TranzrMoves.Application.Features.BusinessAccount.Suspend;
using TranzrMoves.Application.Features.BusinessAccount.Update;
using TranzrMoves.Domain.Entities;
using TranzrMoves.IntegrationTests.Fixtures;
using TranzrMoves.IntegrationTests.TestDoubles;

namespace TranzrMoves.IntegrationTests.BusinessAccount;

public sealed class BusinessAccountIntegrationTests(TestServerFixture fixture)
    : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    static BusinessAccountIntegrationTests()
    {
        JsonOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;

    [Fact]
    public async Task Register_Get_Update_Succeeds_ForOwner()
    {
        var supabaseId = Guid.NewGuid();
        const string ownerEmail = "owner@business.example";

        var registerResponse = await PostRegisterAsync(supabaseId, ownerEmail, CreateRegisterCommand(ownerEmail));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterBusinessAccountResponse>(
            JsonOptions,
            TestContext.Current.CancellationToken);
        registered.Should().NotBeNull();
        registered!.Role.Should().Be(BusinessUserRole.Owner);
        registered.Status.Should().Be(BusinessUserStatus.Active);

        var getResponse = await GetAsync(supabaseId, ownerEmail, registered.BusinessAccountId);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var account = await getResponse.Content.ReadFromJsonAsync<BusinessAccountDto>(
            JsonOptions,
            TestContext.Current.CancellationToken);
        account.Should().NotBeNull();
        account!.BusinessName.Should().Be("Acme Removals");
        account.BusinessAccountId.Should().Be(registered.BusinessAccountId);

        var updateCommand = new UpdateBusinessAccountCommand(
            Id: registered.BusinessAccountId,
            BusinessName: "Acme Removals Ltd",
            TradingName: "Acme",
            BusinessEmail: "billing@acme.example",
            BusinessPhone: "+441234567890",
            BillingAddress: new AddressDto
            {
                Line1 = "99 Updated Street",
                PostCode = "SW1A 2AA",
            },
            CompanyRegistrationNumber: "12345678",
            VatNumber: "GB123456789");

        var updateResponse = await PutAsync(supabaseId, ownerEmail, registered.BusinessAccountId, updateCommand);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<BusinessAccountDto>(
            JsonOptions,
            TestContext.Current.CancellationToken);
        updated!.BusinessName.Should().Be("Acme Removals Ltd");
        updated.BillingAddress.Line1.Should().Be("99 Updated Street");
    }

    [Fact]
    public async Task Get_ReturnsForbidden_WhenUserHasNoBusinessMembership()
    {
        var response = await GetAsync(Guid.NewGuid(), "stranger@example.com", Guid.NewGuid());
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_ReturnsUnauthorized_WhenAuthHeaderMissing()
    {
        var response = await fixture.HttpClient!.PostAsJsonAsync(
            "/api/v1/BusinessAccounts/register",
            CreateRegisterCommand("owner@business.example"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SuspendAndActivate_Work_ViaHandlers()
    {
        var supabaseId = Guid.NewGuid();
        const string ownerEmail = "admin-flow@business.example";

        var registerResponse = await PostRegisterAsync(supabaseId, ownerEmail, CreateRegisterCommand(ownerEmail));
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterBusinessAccountResponse>(
            JsonOptions,
            TestContext.Current.CancellationToken);

        await using var scope = fixture.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var suspendResult = await mediator.Send(
            new SuspendBusinessAccountCommand(registered!.BusinessAccountId),
            TestContext.Current.CancellationToken);
        suspendResult.IsError.Should().BeFalse();
        suspendResult.Value.Status.Should().Be(BusinessAccountStatus.Suspended);

        var activateResult = await mediator.Send(
            new ActivateBusinessAccountCommand(registered.BusinessAccountId),
            TestContext.Current.CancellationToken);
        activateResult.IsError.Should().BeFalse();
        activateResult.Value.Status.Should().Be(BusinessAccountStatus.Active);
    }

    [Fact]
    public async Task Register_PersistsAllThreeEntities_Atomically()
    {
        var supabaseId = Guid.NewGuid();
        const string ownerEmail = "atomic@business.example";

        var registerResponse = await PostRegisterAsync(supabaseId, ownerEmail, CreateRegisterCommand(ownerEmail));
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterBusinessAccountResponse>(
            JsonOptions,
            TestContext.Current.CancellationToken);
        registered.Should().NotBeNull();

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.TranzrMovesDbContext>();
        var ct = TestContext.Current.CancellationToken;

        var user = await db.Set<UserV2>().SingleAsync(x => x.Id == registered!.UserId, ct);
        user.SupabaseId.Should().Be(supabaseId);

        var account = await db.Set<Domain.Entities.BusinessAccount>()
            .SingleAsync(x => x.Id == registered.BusinessAccountId, ct);
        account.Status.Should().Be(BusinessAccountStatus.Active);

        var businessUser = await db.Set<BusinessUser>().SingleAsync(x => x.Id == registered.BusinessUserId, ct);
        businessUser.Role.Should().Be(BusinessUserRole.Owner);
        businessUser.UserId.Should().Be(registered.UserId);
        businessUser.BusinessAccountId.Should().Be(registered.BusinessAccountId);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _resetDatabase();

    private static RegisterBusinessAccountCommand CreateRegisterCommand(string ownerEmail) =>
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

    private Task<HttpResponseMessage> PostRegisterAsync(
        Guid supabaseId,
        string email,
        RegisterBusinessAccountCommand command)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/BusinessAccounts/register")
        {
            Content = JsonContent.Create(command),
        };
        request.Headers.Add(TestAuthDefaults.SupabaseIdHeader, supabaseId.ToString());
        request.Headers.Add(TestAuthDefaults.EmailHeader, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> GetAsync(Guid supabaseId, string email, Guid businessAccountId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/BusinessAccounts/{businessAccountId}");
        request.Headers.Add(TestAuthDefaults.SupabaseIdHeader, supabaseId.ToString());
        request.Headers.Add(TestAuthDefaults.EmailHeader, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> PutAsync(
        Guid supabaseId,
        string email,
        Guid businessAccountId,
        UpdateBusinessAccountCommand command)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/BusinessAccounts/{businessAccountId}")
        {
            Content = JsonContent.Create(command),
        };
        request.Headers.Add(TestAuthDefaults.SupabaseIdHeader, supabaseId.ToString());
        request.Headers.Add(TestAuthDefaults.EmailHeader, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
