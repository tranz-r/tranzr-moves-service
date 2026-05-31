using ErrorOr;
using FluentAssertions;
using NodaTime;
using NSubstitute;
using TranzrMoves.Application.Messaging;
using TranzrMoves.IntegrationTests.Fixtures;

namespace TranzrMoves.IntegrationTests.CheckoutControllerTest;

public sealed class CollectQuoteV2BalanceChargeHandlerTests(PayLaterBalanceChargeMessagingFixture fixture)
    : IClassFixture<PayLaterBalanceChargeMessagingFixture>
{
    [Fact]
    public async Task PublishCollectQuoteV2BalanceCharge_InvokesCollectQuoteV2BalanceChargeHandler()
    {
        var quoteId = Guid.NewGuid();
        var dueDate = SystemClock.Instance.GetCurrentInstant().InUtc().Date;
        var handled = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);

        fixture.CollectionService.TryCollectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                handled.TrySetResult(callInfo.Arg<Guid>());
                return Task.FromResult<ErrorOr<Success>>(Result.Success);
            });

        await fixture.PublishAsync(
            new CollectQuoteV2BalanceCharge(quoteId, dueDate),
            TestContext.Current.CancellationToken);

        var handledQuoteId = await handled.Task.WaitAsync(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);
        handledQuoteId.Should().Be(quoteId);

        await fixture.CollectionService.Received(1)
            .TryCollectAsync(quoteId, Arg.Any<CancellationToken>());
    }
}
