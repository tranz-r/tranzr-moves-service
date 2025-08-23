using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TranzrMoves.IntegrationTests;

public class GuestQuoteControllerTests : IClassFixture<TestingWebAppFactory>
{
    private readonly TestingWebAppFactory _factory;

    public GuestQuoteControllerTests(TestingWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Then_Get_With_IfNoneMatch_Should_Return_304()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // ensure cookie
        var ensure = await client.PostAsync("/api/guest/ensure", new StringContent("{}", Encoding.UTF8, "application/json"));
        ensure.EnsureSuccessStatusCode();

        // initial GET
        var get1 = await client.GetAsync("/api/guest/quote");
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

        // second GET with If-None-Match
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/guest/quote");
        if (!string.IsNullOrEmpty(etag))
        {
            req.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag));
        }
        var get2 = await client.SendAsync(req);
        get2.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task Save_With_Wrong_Etag_Should_Return_412()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // ensure cookie
        var ensure = await client.PostAsync("/api/guest/ensure", new StringContent("{}", Encoding.UTF8, "application/json"));
        ensure.EnsureSuccessStatusCode();

        var body = JsonSerializer.Serialize(new { Quote = "{}", ETag = "W/\"deadbeef\"" });
        var save = await client.PostAsync("/api/guest/quote", new StringContent(body, Encoding.UTF8, "application/json"));
        save.StatusCode.Should().Be((HttpStatusCode)412);
    }
}


