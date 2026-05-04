// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Services;


public sealed class QuoteResumeTokenPayload
{
    public Guid QuoteId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string Purpose { get; set; } = "quote-resume";
}

public interface IQuoteResumeTokenService
{
    string Create(QuoteV2 quote, TimeSpan lifetime);
    QuoteResumeTokenPayload Read(string token);
}

public sealed class QuoteResumeTokenService(IDataProtectionProvider dataProtectionProvider) : IQuoteResumeTokenService
{
    private readonly ITimeLimitedDataProtector _protector = dataProtectionProvider
        .CreateProtector("TranzrMoves.QuoteResume.v1")
        .ToTimeLimitedDataProtector();

    public string Create(QuoteV2 quote, TimeSpan lifetime)
    {
        var payload = new QuoteResumeTokenPayload
        {
            QuoteId = quote.Id,
            SessionId = quote.SessionId
        };

        var json = JsonSerializer.Serialize(payload);
        var protectedValue = _protector.Protect(json, lifetime);
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protectedValue));
    }

    public QuoteResumeTokenPayload Read(string token)
    {
        var protectedBytes = WebEncoders.Base64UrlDecode(token);
        var protectedValue = Encoding.UTF8.GetString(protectedBytes);
        var json = _protector.Unprotect(protectedValue, out _);

        return JsonSerializer.Deserialize<QuoteResumeTokenPayload>(json)
               ?? throw new InvalidOperationException("Invalid resume token.");
    }
}
