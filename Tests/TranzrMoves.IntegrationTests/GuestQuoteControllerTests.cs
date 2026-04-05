using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.IntegrationTests;

public class GuestQuoteControllerTests(TestServerFixture fixture) : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var o = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        o.Converters.Add(new JsonStringEnumConverter());
        o.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        return o;
    }

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;
    private HttpClient Client => fixture.CreateClient();

    [Fact]
    public async Task Get_Then_Get_With_IfNoneMatch_Should_Return_304()
    {
        var client = fixture.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var ensure = await client.PostAsync("/api/v1/quote/ensure", new StringContent("{}", Encoding.UTF8, "application/json"));
        ensure.EnsureSuccessStatusCode();

        var select = await client.PostAsync("/api/v1/quote/select-quote-type?quoteType=Send", null);
        select.EnsureSuccessStatusCode();

        var get1 = await client.GetAsync("/api/v1/quote?quoteType=Send");
        get1.EnsureSuccessStatusCode();
        string? etag = null;
        if (get1.Headers.ETag is not null)
        {
            etag = get1.Headers.ETag.Tag;
        }
        else if (get1.Headers.TryGetValues("ETag", out var etagHeaderValues))
        {
            etag = etagHeaderValues.FirstOrDefault();
        }

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/quote?quoteType=Send");
        if (!string.IsNullOrEmpty(etag))
        {
            var token = etag.Trim('"');
            req.Headers.TryAddWithoutValidation("If-None-Match", $"\"{token}\"");
        }

        var get2 = await client.SendAsync(req);
        get2.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task Save_With_Wrong_Etag_Should_Return_412()
    {
        var client = fixture.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var ensure = await client.PostAsync("/api/v1/quote/ensure", new StringContent("{}", Encoding.UTF8, "application/json"));
        ensure.EnsureSuccessStatusCode();

        var select = await client.PostAsync("/api/v1/quote/select-quote-type?quoteType=Send", null);
        select.EnsureSuccessStatusCode();

        var selectJson = await select.Content.ReadAsStringAsync();
        var selected = JsonSerializer.Deserialize<QuoteTypeDto>(selectJson, JsonOptions);
        selected.Should().NotBeNull();
        selected!.Quote.Should().NotBeNull();

        var saveRequest = new SaveQuoteRequest
        {
            Quote = selected.Quote!,
            ETag = "W/\"deadbeef\""
        };
        var body = JsonSerializer.Serialize(saveRequest, JsonOptions);
        var save = await client.PostAsync("/api/v1/quote", new StringContent(body, Encoding.UTF8, "application/json"));
        save.StatusCode.Should().Be((HttpStatusCode)412);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
