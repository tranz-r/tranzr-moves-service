using NodaTime;
using TranzrMoves.Notifications.Contracts;
using TranzrMoves.Notifications.Infrastructure;
using TranzrMoves.Notifications.Infrastructure.Entities;
using TranzrMoves.Notifications.Infrastructure.Repositories;

namespace TranzrMoves.Notifications.Application.Services;

public sealed class MarketingPreferenceService(
    IMarketingPreferenceRepository repository,
    IClock clock) : IMarketingPreferenceService
{
    public async Task<MarketingPreferenceDto> ApplyPreferencesAsync(
        ApplyMarketingPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = MarketingConsentEmailNormalizer.Normalize(request.Email);
        var now = clock.GetCurrentInstant();
        var preference = await repository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (preference is null)
        {
            preference = new CustomerMarketingPreference
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                CustomerId = request.CustomerId,
                EmailMarketingEnabled = false,
                SmsMarketingEnabled = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            await repository.AddAsync(preference, cancellationToken);
        }
        else if (request.CustomerId is not null && preference.CustomerId is null)
        {
            preference.CustomerId = request.CustomerId;
        }

        await ApplyChannelAsync(
            preference,
            MarketingConsentChannel.Email,
            request.EmailMarketingEnabled,
            request.Source,
            request.IpAddress,
            request.UserAgent,
            now,
            cancellationToken);

        await ApplyChannelAsync(
            preference,
            MarketingConsentChannel.Sms,
            request.SmsMarketingEnabled,
            request.Source,
            request.IpAddress,
            request.UserAgent,
            now,
            cancellationToken);

        preference.UpdatedAt = now;
        await repository.SaveChangesAsync(cancellationToken);

        return ToDto(preference);
    }

    public async Task<MarketingPreferenceDto?> GetByIdAsync(Guid prefId, CancellationToken cancellationToken)
    {
        var preference = await repository.GetByIdAsync(prefId, cancellationToken);
        return preference is null ? null : ToDto(preference);
    }

    public async Task<bool> IsChannelEnabledAsync(
        string email,
        MarketingConsentChannel channel,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = MarketingConsentEmailNormalizer.Normalize(email);
        var preference = await repository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (preference is null)
        {
            return false;
        }

        return channel switch
        {
            MarketingConsentChannel.Email => preference.EmailMarketingEnabled,
            MarketingConsentChannel.Sms => preference.SmsMarketingEnabled,
            _ => false
        };
    }

    public async Task<Guid> GetOrCreatePreferenceIdAsync(
        string email,
        Guid? customerId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = MarketingConsentEmailNormalizer.Normalize(email);
        var preference = await repository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (preference is not null)
        {
            if (customerId is not null && preference.CustomerId is null)
            {
                preference.CustomerId = customerId;
                preference.UpdatedAt = clock.GetCurrentInstant();
                await repository.SaveChangesAsync(cancellationToken);
            }

            return preference.Id;
        }

        var now = clock.GetCurrentInstant();
        preference = new CustomerMarketingPreference
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            CustomerId = customerId,
            EmailMarketingEnabled = false,
            SmsMarketingEnabled = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(preference, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return preference.Id;
    }

    private async Task ApplyChannelAsync(
        CustomerMarketingPreference preference,
        MarketingConsentChannel channel,
        bool enabled,
        MarketingConsentSource source,
        string? ipAddress,
        string? userAgent,
        Instant now,
        CancellationToken cancellationToken)
    {
        var currentEnabled = channel switch
        {
            MarketingConsentChannel.Email => preference.EmailMarketingEnabled,
            MarketingConsentChannel.Sms => preference.SmsMarketingEnabled,
            _ => false
        };

        if (currentEnabled == enabled)
        {
            return;
        }

        switch (channel)
        {
            case MarketingConsentChannel.Email:
                preference.EmailMarketingEnabled = enabled;
                if (enabled)
                {
                    preference.EmailMarketingConsentedAt = now;
                }

                break;
            case MarketingConsentChannel.Sms:
                preference.SmsMarketingEnabled = enabled;
                if (enabled)
                {
                    preference.SmsMarketingConsentedAt = now;
                }

                break;
        }

        await repository.AddEventAsync(new MarketingConsentEvent
        {
            Id = Guid.NewGuid(),
            CustomerMarketingPreferenceId = preference.Id,
            Email = preference.Email,
            Channel = channel,
            EventType = enabled
                ? MarketingConsentEventType.Granted
                : MarketingConsentEventType.Withdrawn,
            Source = source,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            OccurredAt = now
        }, cancellationToken);
    }

    private static MarketingPreferenceDto ToDto(CustomerMarketingPreference preference) =>
        new(
            preference.Id,
            preference.Email,
            preference.EmailMarketingEnabled,
            preference.SmsMarketingEnabled);
}
