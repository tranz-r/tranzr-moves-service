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

namespace TranzrMoves.IntegrationTests.BusinessDashboard;

public sealed class BusinessDashboardIntegrationTests(TestServerFixture fixture)
    : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    static BusinessDashboardIntegrationTests()
    {
        JsonOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;

    [Fact]
    public async Task Summary_ForActiveOwner_ReturnsZeros()
    {
        var owner = await RegisterOwnerAsync();

        var response = await GetAsync(owner.SupabaseId, owner.Email, "summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(
            JsonOptions, TestContext.Current.CancellationToken);
        summary.Should().NotBeNull();
        summary!.UpcomingJobs.Should().Be(0);
        summary.JobsInProgress.Should().Be(0);
        summary.CompletedJobsThisMonth.Should().Be(0);
        summary.OutstandingInvoiceAmount.Should().Be(0m);
    }

    [Fact]
    public async Task ListEndpoints_ForActiveOwner_ReturnEmpty()
    {
        var owner = await RegisterOwnerAsync();

        foreach (var path in new[] { "upcoming-jobs", "active-jobs", "recent-activity" })
        {
            var response = await GetAsync(owner.SupabaseId, owner.Email, path);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var items = await response.Content.ReadFromJsonAsync<List<JsonElement>>(
                JsonOptions, TestContext.Current.CancellationToken);
            items.Should().NotBeNull();
            items!.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task OutstandingInvoices_AsOwner_ReturnsEmpty()
    {
        var owner = await RegisterOwnerAsync();

        var response = await GetAsync(owner.SupabaseId, owner.Email, "outstanding-invoices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoices = await response.Content.ReadFromJsonAsync<List<OutstandingInvoiceDto>>(
            JsonOptions, TestContext.Current.CancellationToken);
        invoices.Should().NotBeNull();
        invoices!.Should().BeEmpty();
    }

    [Fact]
    public async Task OutstandingInvoices_AsFinance_IsAllowed()
    {
        var owner = await RegisterOwnerAsync();
        var finance = await InviteAndActivateAsync(owner, "finance@business.example", BusinessUserRole.Finance);

        var response = await GetAsync(finance.SupabaseId, finance.Email, "outstanding-invoices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OutstandingInvoices_AsMember_ReturnsForbidden()
    {
        var owner = await RegisterOwnerAsync();
        var member = await InviteAndActivateAsync(owner, "member@business.example", BusinessUserRole.Member);

        var response = await GetAsync(member.SupabaseId, member.Email, "outstanding-invoices");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OutstandingInvoices_AsViewer_ReturnsForbidden()
    {
        var owner = await RegisterOwnerAsync();
        var viewer = await InviteAndActivateAsync(owner, "viewer@business.example", BusinessUserRole.Viewer);

        var response = await GetAsync(viewer.SupabaseId, viewer.Email, "outstanding-invoices");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Summary_ForNonMember_ReturnsForbidden()
    {
        var response = await GetAsync(Guid.NewGuid(), "stranger@example.com", "summary");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Summary_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await fixture.HttpClient!.GetAsync(
            "/api/v1/BusinessDashboard/summary",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OutstandingInvoices_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await fixture.HttpClient!.GetAsync(
            "/api/v1/BusinessDashboard/outstanding-invoices",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _resetDatabase();

    private sealed record OwnerContext(Guid SupabaseId, string Email, Guid BusinessAccountId);

    private sealed record UserContext(Guid SupabaseId, string Email);

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
        return new OwnerContext(supabaseId, ownerEmail, registered!.BusinessAccountId);
    }

    private async Task<UserContext> InviteAndActivateAsync(OwnerContext owner, string email, BusinessUserRole role)
    {
        var inviteRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/BusinessUsers/invitations")
        {
            Content = JsonContent.Create(
                new InviteBusinessUserCommand("Test", "User", email, role),
                options: JsonOptions),
        };
        AddAuth(inviteRequest, owner.SupabaseId, owner.Email);
        var inviteResponse = await fixture.HttpClient!.SendAsync(inviteRequest, TestContext.Current.CancellationToken);
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var supabaseId = Guid.NewGuid();
        var acceptRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Auth/accept-invitation");
        AddAuth(acceptRequest, supabaseId, email);
        var acceptResponse = await fixture.HttpClient!.SendAsync(acceptRequest, TestContext.Current.CancellationToken);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return new UserContext(supabaseId, email);
    }

    private Task<HttpResponseMessage> GetAsync(Guid supabaseId, string email, string pathSuffix)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/BusinessDashboard/{pathSuffix}");
        AddAuth(request, supabaseId, email);
        return fixture.HttpClient!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static void AddAuth(HttpRequestMessage request, Guid supabaseId, string email)
    {
        request.Headers.Add(TestAuthDefaults.SupabaseIdHeader, supabaseId.ToString());
        request.Headers.Add(TestAuthDefaults.EmailHeader, email);
    }
}
