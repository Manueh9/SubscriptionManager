// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using SubscriptionManager.API.Infrastructure;

namespace SubscriptionManager.API.Tests;

/// <summary>
/// Pure unit tests of <see cref="StartupSecurity.EnsureLoopbackOnly"/> — no server, no
/// database. Confirms the app refuses to start if configured to listen anywhere but
/// loopback.
/// </summary>
public class StartupSecurityTests
{
    [Theory]
    [InlineData("http://127.0.0.1:5166")]
    [InlineData("http://localhost:5166")]
    [InlineData("https://localhost:7144")]
    [InlineData("http://[::1]:5166")]
    public void EnsureLoopbackOnly_Allows_LoopbackUrls(string url)
    {
        StartupSecurity.EnsureLoopbackOnly([url]);
    }

    [Theory]
    [InlineData("http://0.0.0.0:5166")]
    [InlineData("http://192.168.1.5:5166")]
    [InlineData("http://*:5166")]
    [InlineData("http://+:5166")]
    public void EnsureLoopbackOnly_Rejects_NonLoopbackUrls(string url)
    {
        Assert.Throws<InvalidOperationException>(() => StartupSecurity.EnsureLoopbackOnly([url]));
    }

    [Fact]
    public void EnsureLoopbackOnly_Allows_EmptyList()
    {
        StartupSecurity.EnsureLoopbackOnly([]);
    }
}
