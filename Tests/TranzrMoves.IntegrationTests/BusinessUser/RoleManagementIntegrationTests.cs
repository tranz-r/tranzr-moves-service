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

public sealed class RoleManagementIntegrationTests(TestServerFixture fixture)
    : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    static RoleManagementIntegrationTests()
    {
        JsonOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;

    [Fact]
    public async Task ChangeRole_AsOwner_UpdatesRole_AndWritesAudit()
    {
        var owner = await RegisterOwnerAsync();
        var member = await InviteAndAcceptAsync(owner, "member@business.example", BusinessUserRole.Member);

        var response = await ChangeRoleAsync(owner.SupabaseId, owner.Email, member.BusinessUserId, BusinessUserRole.Finance);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadDtoAsync(response)).Role.Should().Be(BusinessUserRole.Finance);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.TranzrMovesDbContext>();
        var ct = TestContext.Current.CancellationToken;

        var updated = await db.Set<Domain.Entities.BusinessUser>()
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == member.BusinessUserId, ct);
        updated.Role.Should().Be(BusinessUserRole.Finance);
        updated.UpdatedByBusinessUserId.Should().Be(owner.BusinessUserId);

        var audit = await db.Set<BusinessUserRoleChange>()
            .IgnoreQueryFilters()
            .Where(x => x.TargetBusinessUserId == member.BusinessUserId)
            .ToListAsync(ct);
        audit.Should().ContainSingle();
        audit[0].FromRole.Should().Be(BusinessUserRole.Member);
        audit[0].ToRole.Should().Be(BusinessUserRole.Finance);
        audit[0].ChangeType.Should().Be(RoleChangeType.RoleChange);
        audit[0].ChangedByBusinessUserId.Should().Be(owner.BusinessUserId);
    }

    [Fact]
    public async Task ChangeRole_AsAdmin_CanAssignFinance()
    {
        var owner = await RegisterOwnerAsync();
        var admin = await InviteAndAcceptAsync(owner, "admin@business.example", BusinessUserRole.Admin);
        var member = await InviteAndAcceptAsync(owner, "member@business.example", BusinessUserRole.Member);

        var response = await ChangeRoleAsync(admin.SupabaseId, admin.Email, member.BusinessUserId, BusinessUserRole.Finance);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadDtoAsync(response)).Role.Should().Be(BusinessUserRole.Finance);
    }

    [Fact]
    public async Task ChangeRole_AsAdmin_CannotAssignAdmin_ReturnsForbidden()
    {
        var owner = await RegisterOwnerAsync();
        var admin = await InviteAndAcceptAsync(owner, "admin@business.example", BusinessUserRole.Admin);
        var member = await InviteAndAcceptAsync(owner, "member@business.example", BusinessUserRole.Member);

        var response = await ChangeRoleAsync(admin.SupabaseId, admin.Email, member.BusinessUserId, BusinessUserRole.Admin);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeRole_AsAdmin_CannotModifyOwner_ReturnsForbidden()
    {
        var owner = await RegisterOwnerAsync();
        var admin = await InviteAndAcceptAsync(owner, "admin@business.example", BusinessUserRole.Admin);

        var response = await ChangeRoleAsync(admin.SupabaseId, admin.Email, owner.BusinessUserId, BusinessUserRole.Member);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeRole_AsMember_ReturnsForbidden()
    {
        var owner = await RegisterOwnerAsync();
        var actor = await InviteAndAcceptAsync(owner, "actor@business.example", BusinessUserRole.Member);
        var target = await InviteAndAcceptAsync(owner, "target@business.example", BusinessUserRole.Member);

        var response = await ChangeRoleAsync(actor.SupabaseId, actor.Email, target.BusinessUserId, BusinessUserRole.Finance);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeRole_OwnRole_ReturnsBadRequest()
    {
        var owner = await RegisterOwnerAsync();

        var response = await ChangeRoleAsync(owner.SupabaseId, owner.Email, owner.BusinessUserId, BusinessUserRole.Admin);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeRole_ToOwner_ReturnsBadRequest()
    {
        var owner = await RegisterOwnerAsync();
        var member = await InviteAndAcceptAsync(owner, "member@business.example", BusinessUserRole.Member);

        var response = await ChangeRoleAsync(owner.SupabaseId, owner.Email, member.BusinessUserId, BusinessUserRole.Owner);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeRole_CrossTenant_ReturnsNotFound()
    {
        var ownerA = await RegisterOwnerAsync(
            ownerEmail: "owner-a@business.example",
            businessEmail: "billing-a@acme.example",
            businessName: "Acme A");
        var ownerB = await RegisterOwnerAsync(
            ownerEmail: "owner-b@business.example",
            businessEmail: "billing-b@acme.example",
            businessName: "Acme B");
        var memberB = await InviteAndAcceptAsync(ownerB, "member-b@business.example", BusinessUserRole.Member);

        var response = await ChangeRoleAsync(ownerA.SupabaseId, ownerA.Email, memberB.BusinessUserId, BusinessUserRole.Finance);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeRole_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await fixture.HttpClient!.PutAsJsonAsync(
            $"/api/v1/BusinessUsers/{Guid.NewGuid()}/role",
            new ChangeRoleRequest { Role = BusinessUserRole.Finance },
            JsonOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TransferOwnership_AsOwner_SwapsRoles_AndWritesTwoAuditRows()
    {
        var owner = await RegisterOwnerAsync();
        var member = await InviteAndAcceptAsync(owner, "member@business.example", BusinessUserRole.Member);

        var response = await TransferOwnershipAsync(owner.SupabaseId, owner.Email, member.BusinessUserId);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TransferOwnershipResponse>(
            JsonOptions, TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.PreviousOwnerRole.Should().Be(BusinessUserRole.Admin);
        body.NewOwnerRole.Should().Be(BusinessUserRole.Owner);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.TranzrMovesDbContext>();
        var ct = TestContext.Current.CancellationToken;

        var formerOwner = await db.Set<Domain.Entities.BusinessUser>()
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == owner.BusinessUserId, ct);
        formerOwner.Role.Should().Be(BusinessUserRole.Admin);

        var newOwner = await db.Set<Domain.Entities.BusinessUser>()
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == member.BusinessUserId, ct);
        newOwner.Role.Should().Be(BusinessUserRole.Owner);
        newOwner.UpdatedByBusinessUserId.Should().Be(owner.BusinessUserId);

        // AC-014: exactly one active Owner remains.
        var activeOwners = await db.Set<Domain.Entities.BusinessUser>()
            .IgnoreQueryFilters()
            .CountAsync(x => x.BusinessAccountId == owner.BusinessAccountId
                && x.Role == BusinessUserRole.Owner
                && x.Status == BusinessUserStatus.Active, ct);
        activeOwners.Should().Be(1);

        var audit = await db.Set<BusinessUserRoleChange>()
            .IgnoreQueryFilters()
            .Where(x => x.ChangeType == RoleChangeType.OwnershipTransfer
                && x.BusinessAccountId == owner.BusinessAccountId)
            .ToListAsync(ct);
        audit.Should().HaveCount(2);
        audit.Should().ContainSingle(x => x.TargetBusinessUserId == owner.BusinessUserId
            && x.ToRole == BusinessUserRole.Admin);
        audit.Should().ContainSingle(x => x.TargetBusinessUserId == member.BusinessUserId
            && x.ToRole == BusinessUserRole.Owner);
    }

    [Fact]
    public async Task TransferOwnership_AsAdmin_ReturnsForbidden()
    {
        var owner = await RegisterOwnerAsync();
        var admin = await InviteAndAcceptAsync(owner, "admin@business.example", BusinessUserRole.Admin);
        var member = await InviteAndAcceptAsync(owner, "member@business.example", BusinessUserRole.Member);

        var response = await TransferOwnershipAsync(admin.SupabaseId, admin.Email, member.BusinessUserId);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TransferOwnership_ToSelf_ReturnsBadRequest()
    {
        var owner = await RegisterOwnerAsync();

        var response = await TransferOwnershipAsync(owner.SupabaseId, owner.Email, owner.BusinessUserId);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransferOwnership_ToInvitedMember_ReturnsBadRequest()
    {
        var owner = await RegisterOwnerAsync();

        // Invited (not yet accepted) member is not Active.
        var inviteResponse = await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand("Pending", "Member", "pending@business.example", BusinessUserRole.Member));
        var invited = await inviteResponse.Content.ReadFromJsonAsync<InviteBusinessUserResponse>(
            JsonOptions, TestContext.Current.CancellationToken);

        var response = await TransferOwnershipAsync(owner.SupabaseId, owner.Email, invited!.BusinessUserId);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransferOwnership_CrossTenant_ReturnsNotFound()
    {
        var ownerA = await RegisterOwnerAsync(
            ownerEmail: "owner-a@business.example",
            businessEmail: "billing-a@acme.example",
            businessName: "Acme A");
        var ownerB = await RegisterOwnerAsync(
            ownerEmail: "owner-b@business.example",
            businessEmail: "billing-b@acme.example",
            businessName: "Acme B");
        var memberB = await InviteAndAcceptAsync(ownerB, "member-b@business.example", BusinessUserRole.Member);

        var response = await TransferOwnershipAsync(ownerA.SupabaseId, ownerA.Email, memberB.BusinessUserId);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TransferOwnership_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await fixture.HttpClient!.PostAsync(
            $"/api/v1/BusinessUsers/{Guid.NewGuid()}/transfer-ownership",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _resetDatabase();

    private async Task<BusinessUserDto> ReadDtoAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<BusinessUserDto>(
            JsonOptions, TestContext.Current.CancellationToken))!;

    private sealed record OwnerContext(Guid SupabaseId, string Email, Guid BusinessAccountId, Guid BusinessUserId, Guid UserId);

    private sealed record MemberContext(Guid BusinessUserId, Guid SupabaseId, string Email, BusinessUserRole Role);

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

    private async Task<MemberContext> InviteAndAcceptAsync(
        OwnerContext owner,
        string email,
        BusinessUserRole role)
    {
        var inviteResponse = await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand("Test", "Member", email, role));
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var invited = await inviteResponse.Content.ReadFromJsonAsync<InviteBusinessUserResponse>(
            JsonOptions, TestContext.Current.CancellationToken);

        var memberSupabaseId = Guid.NewGuid();
        var acceptResponse = await AcceptInvitationAsync(memberSupabaseId, email);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return new MemberContext(invited!.BusinessUserId, memberSupabaseId, email, role);
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

    private Task<HttpResponseMessage> AcceptInvitationAsync(Guid supabaseId, string email)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Auth/accept-invitation");
        AddAuth(request, supabaseId, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> ChangeRoleAsync(Guid supabaseId, string email, Guid targetId, BusinessUserRole role)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/BusinessUsers/{targetId}/role")
        {
            Content = JsonContent.Create(new ChangeRoleRequest { Role = role }, options: JsonOptions),
        };
        AddAuth(request, supabaseId, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> TransferOwnershipAsync(Guid supabaseId, string email, Guid targetId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/BusinessUsers/{targetId}/transfer-ownership");
        AddAuth(request, supabaseId, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static void AddAuth(HttpRequestMessage request, Guid supabaseId, string email)
    {
        request.Headers.Add(TestAuthDefaults.SupabaseIdHeader, supabaseId.ToString());
        request.Headers.Add(TestAuthDefaults.EmailHeader, email);
    }
}
