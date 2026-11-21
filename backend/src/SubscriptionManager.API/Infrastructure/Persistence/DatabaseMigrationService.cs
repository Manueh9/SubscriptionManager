// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace SubscriptionManager.API.Infrastructure.Persistence;

/// <summary>
/// Backs up the SQLite file before applying any pending migration, so a failed or
/// unwanted schema change can be rolled back by restoring the copy by hand.
/// </summary>
public static class DatabaseMigrationService
{
    private const int MaxBackupsToKeep = 10;

    public static void ApplyPendingMigrations(
        SubscriptionManagerDbContext context,
        string connectionString,
        TimeProvider timeProvider)
    {
        if (!context.Database.GetPendingMigrations().Any())
        {
            return;
        }

        var databaseFilePath = new SqliteConnectionStringBuilder(connectionString).DataSource;

        if (File.Exists(databaseFilePath))
        {
            BackUp(databaseFilePath, timeProvider);
        }

        context.Database.Migrate();
    }

    private static void BackUp(string databaseFilePath, TimeProvider timeProvider)
    {
        var backupDirectory = Path.Combine(Path.GetDirectoryName(databaseFilePath)!, "backups");
        Directory.CreateDirectory(backupDirectory);

        var baseName = Path.GetFileNameWithoutExtension(databaseFilePath);
        var extension = Path.GetExtension(databaseFilePath);
        var timestamp = timeProvider.GetUtcNow().ToString("yyyyMMddHHmmss");

        var backupFilePath = Path.Combine(backupDirectory, $"{baseName}-{timestamp}{extension}");
        File.Copy(databaseFilePath, backupFilePath, overwrite: true);

        PruneOldBackups(backupDirectory, baseName, extension);
    }

    private static void PruneOldBackups(string backupDirectory, string baseName, string extension)
    {
        var backupFiles = Directory.GetFiles(backupDirectory, $"{baseName}-*{extension}")
            .OrderByDescending(path => path, StringComparer.Ordinal)
            .Skip(MaxBackupsToKeep);

        foreach (var oldBackup in backupFiles)
        {
            File.Delete(oldBackup);
        }
    }
}
