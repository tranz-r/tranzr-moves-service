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

namespace TranzrMoves.IntegrationTests.BusinessUser;

public sealed class TeamInvitationsIntegrationTests(TestServerFixture fixture)
    : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    static TeamInvitationsIntegrationTests()
    {
        JsonOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;

    [Fact]
    public async Task Invite_SetsExpiryAndAppearsInPendingList()
    {
        var owner = await RegisterOwnerAsync();
        const string memberEmail = "member@business.example";

        var inviteResponse = await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand(null, null, memberEmail, BusinessUserRole.Member));
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var invited = await ReadInviteAsync(inviteResponse);
        invited.Status.Should().Be(BusinessUserStatus.Invited);
        invited.ExpiresAtUtc.Should().NotBeNull();
        invited.ExpiresAtUtc!.Value.Should().BeGreaterThan(SystemClock.Instance.GetCurrentInstant());

        var invitations = await GetInvitationsAsync(owner.SupabaseId, owner.Email);
        invitations.Should().ContainSingle(i =>
            i.Email == memberEmail
            && i.Role == BusinessUserRole.Member
            && i.Status == BusinessUserStatus.Invited
            && i.IsExpired == false
            && i.InvitedByName == "Jane Owner");
    }

    [Fact]
    public async Task Invite_DuplicatePending_ReturnsConflict()
    {
        var owner = await RegisterOwnerAsync();
        const string memberEmail = "member@business.example";

        var first = await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand(null, null, memberEmail, BusinessUserRole.Member));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand(null, null, memberEmail, BusinessUserRole.Viewer));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Revoke_AsOwner_SetsRevoked_AndAcceptIsForbidden()
    {
        var owner = await RegisterOwnerAsync();
        const string memberEmail = "member@business.example";

        var inviteResponse = await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand(null, null, memberEmail, BusinessUserRole.Member));
        var invited = await ReadInviteAsync(inviteResponse);

        var revoke = await RevokeAsync(owner.SupabaseId, owner.Email, invited.BusinessUserId);
        revoke.StatusCode.Should().Be(HttpStatusCode.OK);
        var revokeResult = await ReadActionAsync(revoke);
        revokeResult.Status.Should().Be(BusinessUserStatus.Revoked);

        // AC-010: a revoked invitation cannot be accepted.
        var accept = await AcceptInvitationAsync(Guid.NewGuid(), memberEmail);
        accept.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // It no longer appears as pending.
        var invitations = await GetInvitationsAsync(owner.SupabaseId, owner.Email);
        invitations.Should().NotContain(i => i.BusinessUserId == invited.BusinessUserId);
    }

    [Fact]
    public async Task Resend_ResetsExpiry_ForExpiredInvitation()
    {
        var owner = await RegisterOwnerAsync();
        const string memberEmail = "member@business.example";

        var inviteResponse = await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand(null, null, memberEmail, BusinessUserRole.Member));
        var invited = await ReadInviteAsync(inviteResponse);

        // Force the invitation to be expired.
        var past = SystemClock.Instance.GetCurrentInstant() - Duration.FromHours(2);
        await SetInvitationExpiryAsync(invited.BusinessUserId, past);

        var resend = await ResendAsync(owner.SupabaseId, owner.Email, invited.BusinessUserId);
        resend.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadActionAsync(resend);
        result.Status.Should().Be(BusinessUserStatus.Invited);
        result.ExpiresAtUtc.Should().NotBeNull();
        result.ExpiresAtUtc!.Value.Should().BeGreaterThan(SystemClock.Instance.GetCurrentInstant());

        var stored = await GetBusinessUserAsync(invited.BusinessUserId);
        stored.InvitationExpiresAt.Should().NotBeNull();
        stored.InvitationExpiresAt!.Value.Should().BeGreaterThan(SystemClock.Instance.GetCurrentInstant());
    }

    [Fact]
    public async Task Accept_ExpiredInvitation_ReturnsForbidden()
    {
        var owner = await RegisterOwnerAsync();
        const string memberEmail = "member@business.example";

        var inviteResponse = await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand(null, null, memberEmail, BusinessUserRole.Member));
        var invited = await ReadInviteAsync(inviteResponse);

        var past = SystemClock.Instance.GetCurrentInstant() - Duration.FromHours(2);
        await SetInvitationExpiryAsync(invited.BusinessUserId, past);

        // AC-009: an expired invitation cannot be accepted.
        var accept = await AcceptInvitationAsync(Guid.NewGuid(), memberEmail);
        accept.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CrossTenant_RevokeAndResend_ReturnNotFound()
    {
        var ownerA = await RegisterOwnerAsync(
            ownerEmail: "owner-a@business.example",
            businessEmail: "billing-a@acme.example",
            businessName: "Acme A");
        var ownerB = await RegisterOwnerAsync(
            ownerEmail: "owner-b@business.example",
            businessEmail: "billing-b@acme.example",
            businessName: "Acme B");

        var inviteResponse = await InviteAsync(
            ownerB.SupabaseId,
            ownerB.Email,
            new InviteBusinessUserCommand(null, null, "member-b@business.example", BusinessUserRole.Member));
        var invited = await ReadInviteAsync(inviteResponse);

        // AC-016: Owner A cannot revoke/resend Owner B's invitation.
        var revoke = await RevokeAsync(ownerA.SupabaseId, ownerA.Email, invited.BusinessUserId);
        revoke.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var resend = await ResendAsync(ownerA.SupabaseId, ownerA.Email, invited.BusinessUserId);
        resend.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Member_CannotManageInvitations()
    {
        var owner = await RegisterOwnerAsync();
        const string memberEmail = "member@business.example";

        var inviteResponse = await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand(null, null, memberEmail, BusinessUserRole.Member));
        var invited = await ReadInviteAsync(inviteResponse);

        // Activate the member by accepting their invitation.
        var memberSupabaseId = Guid.NewGuid();
        var accept = await AcceptInvitationAsync(memberSupabaseId, memberEmail);
        accept.StatusCode.Should().Be(HttpStatusCode.OK);

        // AC-003: a Member is not Owner/Admin and cannot manage invitations.
        var list = await GetInvitationsRawAsync(memberSupabaseId, memberEmail);
        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var invite = await InviteAsync(
            memberSupabaseId,
            memberEmail,
            new InviteBusinessUserCommand(null, null, "another@business.example", BusinessUserRole.Viewer));
        invite.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var revoke = await RevokeAsync(memberSupabaseId, memberEmail, invited.BusinessUserId);
        revoke.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var resend = await ResendAsync(memberSupabaseId, memberEmail, invited.BusinessUserId);
        resend.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _resetDatabase();

    private async Task<InviteBusinessUserResponse> ReadInviteAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<InviteBusinessUserResponse>(
            JsonOptions, TestContext.Current.CancellationToken))!;

    private async Task<InvitationActionResponse> ReadActionAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<InvitationActionResponse>(
            JsonOptions, TestContext.Current.CancellationToken))!;

    private async Task<List<InvitationDto>> GetInvitationsAsync(Guid supabaseId, string email)
    {
        var response = await GetInvitationsRawAsync(supabaseId, email);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<InvitationDto>>(
            JsonOptions, TestContext.Current.CancellationToken))!;
    }

    private async Task SetInvitationExpiryAsync(Guid businessUserId, Instant expiresAt)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.TranzrMovesDbContext>();
        var ct = TestContext.Current.CancellationToken;

        var businessUser = await db.Set<Domain.Entities.BusinessUser>()
            .AsTracking()
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == businessUserId, ct);
        businessUser.InvitationExpiresAt = expiresAt;
        await db.SaveChangesAsync(ct);
    }

    private async Task<Domain.Entities.BusinessUser> GetBusinessUserAsync(Guid businessUserId)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.TranzrMovesDbContext>();
        var ct = TestContext.Current.CancellationToken;

        return await db.Set<Domain.Entities.BusinessUser>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(x => x.Id == businessUserId, ct);
    }

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

    private Task<HttpResponseMessage> GetInvitationsRawAsync(Guid supabaseId, string email)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/BusinessUsers/invitations");
        AddAuth(request, supabaseId, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> RevokeAsync(Guid supabaseId, string email, Guid businessUserId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/BusinessUsers/invitations/{businessUserId}/revoke");
        AddAuth(request, supabaseId, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> ResendAsync(Guid supabaseId, string email, Guid businessUserId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/BusinessUsers/invitations/{businessUserId}/resend");
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
