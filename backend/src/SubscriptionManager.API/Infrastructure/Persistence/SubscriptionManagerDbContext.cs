// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using Microsoft.EntityFrameworkCore;
using SubscriptionManager.API.Domain.Entities;

namespace SubscriptionManager.API.Infrastructure.Persistence;

public class SubscriptionManagerDbContext(DbContextOptions<SubscriptionManagerDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubscriptionManagerDbContext).Assembly);
    }
}
