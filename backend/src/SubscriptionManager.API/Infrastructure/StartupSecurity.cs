// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace SubscriptionManager.API.Infrastructure;

/// <summary>
/// Refuses to start the app if it's configured to listen on anything other than the
/// local loopback interface — this API has no authentication, so it must never be
/// reachable from the network.
/// </summary>
public static class StartupSecurity
{
    private static readonly string[] AllowedHosts = ["localhost", "127.0.0.1", "::1", "[::1]"];

    public static void EnsureLoopbackOnly(IEnumerable<string> urls)
    {
        foreach (var url in urls)
        {
            var host = BindingAddress.Parse(url).Host;

            if (!AllowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"La aplicación solo puede escuchar en localhost/127.0.0.1, no en '{host}' " +
                    $"(URL configurada: '{url}'). Revisa ASPNETCORE_URLS o launchSettings.json.");
            }
        }
    }
}
