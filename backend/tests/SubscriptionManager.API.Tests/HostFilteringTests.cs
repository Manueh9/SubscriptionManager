// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using System.Net;

namespace SubscriptionManager.API.Tests;

/// <summary>
/// HTTP-level regression test for the <c>AllowedHosts</c> configuration — confirms a
/// request with an unrecognized <c>Host</c> header is rejected before it ever reaches a
/// controller, which is what guards against DNS-rebinding attacks from a browser tab.
/// </summary>
public class HostFilteringTests
{
    [Fact]
    public async Task Request_WithDisallowedHost_Returns400()
    {
        using var factory = new WebApiFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Categories");
        request.Headers.Host = "evil.example";

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithAllowedHost_Succeeds()
    {
        using var factory = new WebApiFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Categories");
        request.Headers.Host = "localhost";

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
