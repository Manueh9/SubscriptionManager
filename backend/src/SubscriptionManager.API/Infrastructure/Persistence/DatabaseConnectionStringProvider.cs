// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

namespace SubscriptionManager.API.Infrastructure.Persistence;

public static class DatabaseConnectionStringProvider
{
    public static string Resolve(string? configuredConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return configuredConnectionString;
        }

        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SubscriptionManager");

        Directory.CreateDirectory(appDataDirectory);

        var databaseFilePath = Path.Combine(appDataDirectory, "data.db");

        return $"Data Source={databaseFilePath}";
    }
}
