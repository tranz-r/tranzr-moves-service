using System.Diagnostics.Metrics;

namespace TranzrMoves.Notifications.Application.Telemetry;

public sealed class NotificationsMetrics
{
    public const string MeterName = "TranzrMoves.Notifications";

    private static readonly Meter Meter = new(MeterName);

    private readonly Counter<long> _deliverySucceeded;
    private readonly Counter<long> _deliveryFailed;
    private readonly Counter<long> _deliverySkipped;
    private readonly Counter<long> _marketingSent;
    private readonly Counter<long> _marketingBlocked;

    public NotificationsMetrics()
    {
        _deliverySucceeded = Meter.CreateCounter<long>("notifications.delivery.succeeded");
        _deliveryFailed = Meter.CreateCounter<long>("notifications.delivery.failed");
        _deliverySkipped = Meter.CreateCounter<long>("notifications.delivery.skipped");
        _marketingSent = Meter.CreateCounter<long>("notifications.marketing.sent");
        _marketingBlocked = Meter.CreateCounter<long>("notifications.marketing.blocked");
    }

    public void RecordDeliverySucceeded(string category, string templateKey) =>
        _deliverySucceeded.Add(1, CreateTags(category, templateKey));

    public void RecordDeliveryFailed(string category, string templateKey) =>
        _deliveryFailed.Add(1, CreateTags(category, templateKey));

    public void RecordDeliverySkipped(string category, string templateKey, string reason) =>
        _deliverySkipped.Add(1, CreateTags(category, templateKey, reason));

    public void RecordMarketingSent(string templateKey) =>
        _marketingSent.Add(1, CreateTags("Marketing", templateKey));

    public void RecordMarketingBlocked(string templateKey) =>
        _marketingBlocked.Add(1, CreateTags("Marketing", templateKey));

    private static KeyValuePair<string, object?>[] CreateTags(
        string category,
        string templateKey,
        string? reason = null)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("category", category),
            new("template_key", templateKey)
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            tags.Add(new KeyValuePair<string, object?>("reason", reason));
        }

        return tags.ToArray();
    }
}
