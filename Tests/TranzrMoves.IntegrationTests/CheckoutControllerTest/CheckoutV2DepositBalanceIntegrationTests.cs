using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NodaTime;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.IntegrationTests.CheckoutControllerTest;

public class CheckoutV2DepositBalanceIntegrationTests(TestServerFixture fixture)
    : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;
    private HttpClient Client => fixture.CreateClient();

    [Fact]
    public async Task DepositBalance_ReturnsNotFound_When_QuoteV2_Does_Not_Exist()
    {
        var request = new FuturePaymentRequest { QuoteReference = "NO-SUCH-V2-REF-XXXX" };

        var response = await Client.PostAsJsonAsync(
            "/api/v2/checkout/deposit-balance-payment",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DepositBalance_Returns400_When_Quote_Not_PartiallyPaid()
    {
        var db = fixture.DbContext!;
        db.ChangeTracker.Clear();

        var now = SystemClock.Instance.GetCurrentInstant();
        var userId = Guid.NewGuid();
        db.Set<UserV2>().Add(new UserV2
        {
            Id = userId,
            Email = "bal-pending@tranzrmoves.com",
            FirstName = "P",
            LastName = "End",
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "test",
            ModifiedBy = "test"
        });

        var quoteRef = $"PV2-{Guid.NewGuid():N}"[..16];
        db.Set<QuoteV2>().Add(new QuoteV2
        {
            Id = Guid.NewGuid(),
            SessionId = $"pv2-{Guid.NewGuid():N}"[..14],
            Type = QuoteType.Removals,
            QuoteReference = quoteRef,
            VanType = VanType.largeVan,
            CrewCount = 1,
            TotalCost = 100m,
            PaymentStatus = PaymentStatus.Pending,
            CustomerId = userId,
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "test",
            ModifiedBy = "test"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new FuturePaymentRequest { QuoteReference = quoteRef };

        var response = await Client.PostAsJsonAsync(
            "/api/v2/checkout/deposit-balance-payment",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public ValueTask InitializeAsync() => new(Task.CompletedTask);

    public async ValueTask DisposeAsync() => await _resetDatabase();
}
