using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.BusinessAccount.Register;
using TranzrMoves.Application.Features.BusinessUser.Invite;
using TranzrMoves.Domain.Entities;
using TranzrMoves.IntegrationTests.Fixtures;
using TranzrMoves.IntegrationTests.TestDoubles;

namespace TranzrMoves.IntegrationTests.Auth;

public sealed class AuthenticationIntegrationTests(TestServerFixture fixture)
    : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    static AuthenticationIntegrationTests()
    {
        JsonOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;

    [Fact]
    public async Task Context_ForActiveOwner_ReturnsAuthContext()
    {
        var owner = await RegisterOwnerAsync();

        var response = await GetContextAsync(owner.SupabaseId, owner.Email);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var context = await response.Content.ReadFromJsonAsync<AuthContextDto>(
            JsonOptions, TestContext.Current.CancellationToken);
        context.Should().NotBeNull();
        context!.UserId.Should().Be(owner.UserId);
        context.BusinessUserId.Should().Be(owner.BusinessUserId);
        context.BusinessAccountId.Should().Be(owner.BusinessAccountId);
        context.Role.Should().Be(BusinessUserRole.Owner);
        context.Email.Should().Be(owner.Email);
    }

    [Fact]
    public async Task Context_ForNonMember_ReturnsForbidden()
    {
        var response = await GetContextAsync(Guid.NewGuid(), "stranger@example.com");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Context_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await fixture.HttpClient!.GetAsync(
            "/api/v1/Auth/context",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AcceptInvitation_LinksSupabaseId_AndActivatesMembership()
    {
        var owner = await RegisterOwnerAsync();
        const string memberEmail = "member@business.example";

        var inviteResponse = await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand("Mary", "Member", memberEmail, BusinessUserRole.Member));
        var invited = await inviteResponse.Content.ReadFromJsonAsync<InviteBusinessUserResponse>(
            JsonOptions, TestContext.Current.CancellationToken);
        var memberBusinessUserId = invited!.BusinessUserId;

        var memberSupabaseId = Guid.NewGuid();
        var acceptResponse = await AcceptInvitationAsync(memberSupabaseId, memberEmail);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var context = await acceptResponse.Content.ReadFromJsonAsync<AuthContextDto>(
            JsonOptions, TestContext.Current.CancellationToken);
        context.Should().NotBeNull();
        context!.BusinessUserId.Should().Be(memberBusinessUserId);
        context.BusinessAccountId.Should().Be(owner.BusinessAccountId);
        context.Role.Should().Be(BusinessUserRole.Member);
        context.Email.Should().Be(memberEmail);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.TranzrMovesDbContext>();
        var ct = TestContext.Current.CancellationToken;

        var user = await db.Set<UserV2>().SingleAsync(x => x.Email == memberEmail, ct);
        user.SupabaseId.Should().Be(memberSupabaseId);

        var businessUser = await db.Set<Domain.Entities.BusinessUser>()
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == memberBusinessUserId, ct);
        businessUser.Status.Should().Be(BusinessUserStatus.Active);

        // The newly active member can now resolve their own context.
        var contextResponse = await GetContextAsync(memberSupabaseId, memberEmail);
        contextResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AcceptInvitation_WhenAlreadyActive_IsIdempotent()
    {
        var owner = await RegisterOwnerAsync();

        // The owner is already Active and linked; accepting is a no-op that returns context.
        var response = await AcceptInvitationAsync(owner.SupabaseId, owner.Email);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var context = await response.Content.ReadFromJsonAsync<AuthContextDto>(
            JsonOptions, TestContext.Current.CancellationToken);
        context!.BusinessUserId.Should().Be(owner.BusinessUserId);
        context.Role.Should().Be(BusinessUserRole.Owner);
    }

    [Fact]
    public async Task AcceptInvitation_WhenNoInvitation_ReturnsNotFound()
    {
        var response = await AcceptInvitationAsync(Guid.NewGuid(), "nobody@business.example");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcceptInvitation_WhenEmailLinkedToAnotherAccount_ReturnsConflict()
    {
        var owner = await RegisterOwnerAsync();
        const string memberEmail = "member@business.example";

        await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand("Mary", "Member", memberEmail, BusinessUserRole.Member));

        var firstAccept = await AcceptInvitationAsync(Guid.NewGuid(), memberEmail);
        firstAccept.StatusCode.Should().Be(HttpStatusCode.OK);

        // A different Supabase identity claiming the same email must be rejected.
        var secondAccept = await AcceptInvitationAsync(Guid.NewGuid(), memberEmail);
        secondAccept.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AcceptInvitation_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await fixture.HttpClient!.PostAsync(
            "/api/v1/Auth/accept-invitation",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _resetDatabase();

    private sealed record OwnerContext(Guid SupabaseId, string Email, Guid BusinessAccountId, Guid BusinessUserId, Guid UserId);

    private async Task<OwnerContext> RegisterOwnerAsync(
        string ownerEmail = "owner@business.example",
        string businessEmail = "billing@acme.example",
        string businessName = "Acme Removals")
    {
        var supabaseId = Guid.NewGuid();
        var command = new RegisterBusinessAccountCommand(
            BusinessName: businessName,
            TradingName: "Acme",
            BusinessEmail: businessEmail,
            BusinessPhone: "+441234567890",
            BillingAddress: new AddressDto { Line1 = "1 High Street", PostCode = "SW1A 1AA" },
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

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/BusinessAccounts/register")
        {
            Content = JsonContent.Create(command, options: JsonOptions),
        };
        AddAuth(request, supabaseId, ownerEmail);

        var response = await fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var registered = await response.Content.ReadFromJsonAsync<RegisterBusinessAccountResponse>(
            JsonOptions, TestContext.Current.CancellationToken);
        return new OwnerContext(
            supabaseId,
            ownerEmail,
            registered!.BusinessAccountId,
            registered.BusinessUserId,
            registered.UserId);
    }

    private Task<HttpResponseMessage> InviteAsync(Guid supabaseId, string email, InviteBusinessUserCommand command)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/BusinessUsers/invitations")
        {
            Content = JsonContent.Create(command, options: JsonOptions),
        };
        AddAuth(request, supabaseId, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> GetContextAsync(Guid supabaseId, string email)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/Auth/context");
        AddAuth(request, supabaseId, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> AcceptInvitationAsync(Guid supabaseId, string email)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Auth/accept-invitation");
        AddAuth(request, supabaseId, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static void AddAuth(HttpRequestMessage request, Guid supabaseId, string email)
    {
        request.Headers.Add(TestAuthDefaults.SupabaseIdHeader, supabaseId.ToString());
        request.Headers.Add(TestAuthDefaults.EmailHeader, email);
    }
}
