using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.BusinessAccount.Register;
using TranzrMoves.Application.Features.BusinessUser.Invite;
using TranzrMoves.Domain.Entities;
using TranzrMoves.IntegrationTests.Fixtures;
using TranzrMoves.IntegrationTests.TestDoubles;

namespace TranzrMoves.IntegrationTests.BusinessUser;

public sealed class BusinessUserIntegrationTests(TestServerFixture fixture)
    : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    static BusinessUserIntegrationTests()
    {
        JsonOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;

    [Fact]
    public async Task Invite_AsOwner_CreatesInvitedMember_AndListReturnsBoth()
    {
        var owner = await RegisterOwnerAsync();

        var inviteResponse = await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand("Mary", "Member", "member@business.example", BusinessUserRole.Member));
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var invited = await inviteResponse.Content.ReadFromJsonAsync<InviteBusinessUserResponse>(
            JsonOptions, TestContext.Current.CancellationToken);
        invited.Should().NotBeNull();
        invited!.Status.Should().Be(BusinessUserStatus.Invited);

        var listResponse = await GetListAsync(owner.SupabaseId, owner.Email);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await listResponse.Content.ReadFromJsonAsync<List<BusinessUserDto>>(
            JsonOptions, TestContext.Current.CancellationToken);
        users.Should().NotBeNull();
        users!.Should().HaveCount(2);
        users.Should().ContainSingle(u => u.Role == BusinessUserRole.Owner && u.Status == BusinessUserStatus.Active);
        users.Should().ContainSingle(u =>
            u.Email == "member@business.example"
            && u.Role == BusinessUserRole.Member
            && u.Status == BusinessUserStatus.Invited);
    }

    [Fact]
    public async Task Invite_AsNonMember_ReturnsForbidden()
    {
        var response = await InviteAsync(
            Guid.NewGuid(),
            "stranger@example.com",
            new InviteBusinessUserCommand("No", "Access", "victim@business.example", BusinessUserRole.Member));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Invite_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await fixture.HttpClient!.PostAsJsonAsync(
            "/api/v1/BusinessUsers/invitations",
            new InviteBusinessUserCommand("No", "Auth", "noauth@business.example", BusinessUserRole.Member),
            JsonOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await fixture.HttpClient!.GetAsync(
            "/api/v1/BusinessUsers",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_CrossTenant_ReturnsNotFound()
    {
        var ownerA = await RegisterOwnerAsync(
            ownerEmail: "owner-a@business.example",
            businessEmail: "billing-a@acme.example",
            businessName: "Acme A");
        var ownerB = await RegisterOwnerAsync(
            ownerEmail: "owner-b@business.example",
            businessEmail: "billing-b@acme.example",
            businessName: "Acme B");

        // Owner A tries to read Owner B's membership: tenant filter hides it.
        var response = await GetByIdAsync(ownerA.SupabaseId, ownerA.Email, ownerB.BusinessUserId);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SuspendActivateDeactivate_InvitedMember_AsOwner_TransitionsStatus()
    {
        var owner = await RegisterOwnerAsync();

        var inviteResponse = await InviteAsync(
            owner.SupabaseId,
            owner.Email,
            new InviteBusinessUserCommand("Mary", "Member", "member@business.example", BusinessUserRole.Member));
        var invited = await inviteResponse.Content.ReadFromJsonAsync<InviteBusinessUserResponse>(
            JsonOptions, TestContext.Current.CancellationToken);
        var memberId = invited!.BusinessUserId;

        var suspended = await PostActionAsync(owner.SupabaseId, owner.Email, $"{memberId}/suspend");
        suspended.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadDtoAsync(suspended)).Status.Should().Be(BusinessUserStatus.Suspended);

        var activated = await PostActionAsync(owner.SupabaseId, owner.Email, $"{memberId}/activate");
        activated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadDtoAsync(activated)).Status.Should().Be(BusinessUserStatus.Active);

        var deactivated = await PostActionAsync(owner.SupabaseId, owner.Email, $"{memberId}/deactivate");
        deactivated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadDtoAsync(deactivated)).Status.Should().Be(BusinessUserStatus.Deactivated);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _resetDatabase();

    private async Task<BusinessUserDto> ReadDtoAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<BusinessUserDto>(
            JsonOptions, TestContext.Current.CancellationToken))!;

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

    private Task<HttpResponseMessage> GetListAsync(Guid supabaseId, string email)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/BusinessUsers");
        AddAuth(request, supabaseId, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> GetByIdAsync(Guid supabaseId, string email, Guid businessUserId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/BusinessUsers/{businessUserId}");
        AddAuth(request, supabaseId, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> PostActionAsync(Guid supabaseId, string email, string pathSuffix)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/BusinessUsers/{pathSuffix}");
        AddAuth(request, supabaseId, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static void AddAuth(HttpRequestMessage request, Guid supabaseId, string email)
    {
        request.Headers.Add(TestAuthDefaults.SupabaseIdHeader, supabaseId.ToString());
        request.Headers.Add(TestAuthDefaults.EmailHeader, email);
    }
}
