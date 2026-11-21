// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SubscriptionManager.API.Domain.Entities;
using SubscriptionManager.API.Domain.Enum;
using SubscriptionManager.API.Infrastructure.Persistence;

namespace SubscriptionManager.API.Tests;

/// <summary>
/// Tests <see cref="SubscriptionManagerDbContextExtensions.TrySaveChangesAsync"/> against a real
/// SQLite database rather than a crafted exception, so it also confirms that SQLite genuinely
/// reports these violations as SQLITE_CONSTRAINT — the assumption the helper relies on.
/// </summary>
public class TrySaveChangesTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TrySaveChangesTests()
    {
        _connection.Open();

        using var dbContext = NewDbContext();
        dbContext.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    // A fresh context per logical operation, sharing one in-memory database. This mirrors the
    // real per-request DbContext lifetime: a controller only loads what it needs, so EF's
    // change tracker doesn't pre-empt constraints that the database itself should enforce.
    private SubscriptionManagerDbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<SubscriptionManagerDbContext>().UseSqlite(_connection).Options);

    private async Task<Category> SeedCategoryAsync(string name)
    {
        await using var dbContext = NewDbContext();
        var category = new Category(name);
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        return category;
    }

    private async Task SeedSubscriptionAsync(Category category, string name)
    {
        await using var dbContext = NewDbContext();
        dbContext.Attach(category);
        dbContext.Subscriptions.Add(new Subscription(name, "d", 9.99m, BillingCycle.Monthly, DateTime.UtcNow, category));
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task TrySaveChangesAsync_ReturnsTrue_WhenThereIsNothingToSave()
    {
        await using var dbContext = NewDbContext();

        Assert.True(await dbContext.TrySaveChangesAsync());
    }

    [Fact]
    public async Task TrySaveChangesAsync_ReturnsTrue_OnASuccessfulSave()
    {
        await using var dbContext = NewDbContext();
        dbContext.Categories.Add(new Category($"Streaming-{Guid.NewGuid()}"));

        Assert.True(await dbContext.TrySaveChangesAsync());
    }

    [Fact]
    public async Task TrySaveChangesAsync_ReturnsFalse_OnADuplicateCategoryName()
    {
        var name = $"Streaming-{Guid.NewGuid()}";
        await SeedCategoryAsync(name);

        await using var dbContext = NewDbContext();
        dbContext.Categories.Add(new Category(name));

        Assert.False(await dbContext.TrySaveChangesAsync());
    }

    [Fact]
    public async Task TrySaveChangesAsync_ReturnsFalse_OnADuplicateCategoryNameDifferingOnlyInCase()
    {
        // The unique index uses NOCASE collation, so this is the same conflict as above.
        var name = $"Streaming-{Guid.NewGuid()}";
        await SeedCategoryAsync(name);

        await using var dbContext = NewDbContext();
        dbContext.Categories.Add(new Category(name.ToUpperInvariant()));

        Assert.False(await dbContext.TrySaveChangesAsync());
    }

    [Fact]
    public async Task TrySaveChangesAsync_ReturnsFalse_OnADuplicateSubscriptionNameInTheSameCategory()
    {
        var category = await SeedCategoryAsync($"Streaming-{Guid.NewGuid()}");
        await SeedSubscriptionAsync(category, "Netflix");

        await using var dbContext = NewDbContext();
        dbContext.Attach(category);
        dbContext.Subscriptions.Add(new Subscription("Netflix", "d", 9.99m, BillingCycle.Monthly, DateTime.UtcNow, category));

        Assert.False(await dbContext.TrySaveChangesAsync());
    }

    [Fact]
    public async Task TrySaveChangesAsync_ReturnsTrue_ForTheSameSubscriptionNameInADifferentCategory()
    {
        var streaming = await SeedCategoryAsync($"Streaming-{Guid.NewGuid()}");
        var software = await SeedCategoryAsync($"Software-{Guid.NewGuid()}");
        await SeedSubscriptionAsync(streaming, "Netflix");

        await using var dbContext = NewDbContext();
        dbContext.Attach(software);
        dbContext.Subscriptions.Add(new Subscription("Netflix", "d", 9.99m, BillingCycle.Monthly, DateTime.UtcNow, software));

        Assert.True(await dbContext.TrySaveChangesAsync());
    }

    [Fact]
    public async Task TrySaveChangesAsync_ReturnsFalse_WhenDeletingACategoryThatStillHasSubscriptions()
    {
        // The foreign key is ON DELETE RESTRICT, so this is a constraint violation too. The
        // delete runs from a context that hasn't loaded the subscriptions (exactly like
        // CategoriesController.Delete), so SQLite is what rejects it.
        var category = await SeedCategoryAsync($"Streaming-{Guid.NewGuid()}");
        await SeedSubscriptionAsync(category, "Netflix");

        await using var dbContext = NewDbContext();
        dbContext.Categories.Remove(await dbContext.Categories.FirstAsync(c => c.Id == category.Id));

        Assert.False(await dbContext.TrySaveChangesAsync());
    }
}
