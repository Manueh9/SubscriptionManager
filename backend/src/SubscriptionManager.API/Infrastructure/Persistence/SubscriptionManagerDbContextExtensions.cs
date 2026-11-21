// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using Microsoft.EntityFrameworkCore;

namespace SubscriptionManager.API.Infrastructure.Persistence;

public static class SubscriptionManagerDbContextExtensions
{
    /// <summary>
    /// Saves changes, returning false instead of throwing when the save violates a database
    /// constraint — which happens when two concurrent requests both get past a controller's
    /// duplicate pre-check and the unique index catches the loser. Any other failure still
    /// propagates, so a transient error isn't mistaken for a duplicate.
    /// </summary>
    public static async Task<bool> TrySaveChangesAsync(this SubscriptionManagerDbContext dbContext)
    {
        try
        {
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException exception) when (exception.IsConstraintViolation())
        {
            return false;
        }
    }
}
