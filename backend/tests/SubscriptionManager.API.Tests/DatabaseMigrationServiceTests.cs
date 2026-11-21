// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SubscriptionManager.API.Infrastructure.Persistence;

namespace SubscriptionManager.API.Tests;

/// <summary>
/// Pure file-system + EF Core test of <see cref="DatabaseMigrationService"/> — no HTTP
/// server. Simulates an install left on an older schema version to confirm a backup is
/// made before migrating, and that only the 10 most recent backups survive.
/// </summary>
public class DatabaseMigrationServiceTests : IDisposable
{
    private readonly string _tempDirectory =
        Path.Combine(Path.GetTempPath(), $"subscription-manager-tests-{Guid.NewGuid():N}");

    public DatabaseMigrationServiceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void ApplyPendingMigrations_BacksUpAndMigrates_WhenAMigrationIsPending()
    {
        var databaseFilePath = Path.Combine(_tempDirectory, "data.db");
        var connectionString = $"Data Source={databaseFilePath}";

        // Leave the database on the first migration only, simulating an older install.
        using (var context = CreateContext(connectionString))
        {
            context.GetService<IMigrator>().Migrate("20261031181752_InitialCreate");
        }

        var backupDirectory = Path.Combine(_tempDirectory, "backups");
        Directory.CreateDirectory(backupDirectory);
        for (var i = 0; i < 10; i++)
        {
            File.WriteAllText(Path.Combine(backupDirectory, $"data-2020010100000{i}.db"), "old-backup");
        }

        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 11, 21, 9, 0, 0, TimeSpan.Zero));

        using (var context = CreateContext(connectionString))
        {
            DatabaseMigrationService.ApplyPendingMigrations(context, connectionString, timeProvider);
        }

        var backupFiles = Directory.GetFiles(backupDirectory, "data-*.db");
        Assert.Equal(10, backupFiles.Length);
        Assert.Contains(backupFiles, f => f.Contains("20261121090000"));
        Assert.DoesNotContain(backupFiles, f => f.Contains("data-20200101000000.db"));

        using var finalContext = CreateContext(connectionString);
        Assert.Empty(finalContext.Database.GetPendingMigrations());
    }

    [Fact]
    public void ApplyPendingMigrations_DoesNothing_WhenNoMigrationIsPending()
    {
        var databaseFilePath = Path.Combine(_tempDirectory, "data.db");
        var connectionString = $"Data Source={databaseFilePath}";

        using (var context = CreateContext(connectionString))
        {
            context.Database.Migrate();
        }

        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 11, 21, 9, 0, 0, TimeSpan.Zero));

        using var context2 = CreateContext(connectionString);
        DatabaseMigrationService.ApplyPendingMigrations(context2, connectionString, timeProvider);

        Assert.False(Directory.Exists(Path.Combine(_tempDirectory, "backups")));
    }

    private static SubscriptionManagerDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<SubscriptionManagerDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new SubscriptionManagerDbContext(options);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
