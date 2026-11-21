// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

namespace SubscriptionManager.API.Domain.Entities;

public class Category
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public Category(string name)
    {
        EnsureIsValid(name);

        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        EnsureIsValid(name);
        Name = name;
    }

    private static void EnsureIsValid(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
    }

    private Category()
    {
        // Required by EF Core.
    }
}
