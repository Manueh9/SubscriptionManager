// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubscriptionManager.API.DTO;
using SubscriptionManager.API.Domain.Enum;
using SubscriptionManager.API.Infrastructure.Persistence;

namespace SubscriptionManager.API.Controllers;

/// <summary>
/// Reports aggregated spending across subscriptions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExpensesController(SubscriptionManagerDbContext dbContext) : ControllerBase
{
    #region Summary

    /// <summary>
    /// Gets the total monthly and yearly cost of all active subscriptions. Paused and
    /// cancelled subscriptions don't count towards it.
    /// </summary>
    /// <returns>200 with the expense summary.</returns>
    [HttpGet("summary")]
    public async Task<ActionResult<ExpensesSummaryResponse>> GetSummary()
    {
        var activeSubscriptions = await dbContext.Subscriptions
            .Where(subscription => subscription.Status == SubscriptionStatus.Active)
            .Select(subscription => new { subscription.Price, subscription.BillingCycle })
            .ToListAsync();

        var yearlyTotal = activeSubscriptions.Sum(subscription => ToYearlyCost(subscription.Price, subscription.BillingCycle));

        return Ok(new ExpensesSummaryResponse(
            MonthlyTotal: Math.Round(yearlyTotal / 12, 2),
            YearlyTotal: Math.Round(yearlyTotal, 2),
            ActiveSubscriptionCount: activeSubscriptions.Count));
    }

    #endregion

    private static decimal ToYearlyCost(decimal price, BillingCycle billingCycle) => billingCycle switch
    {
        BillingCycle.Day => price * 365,
        BillingCycle.Monthly => price * 12,
        BillingCycle.Yearly => price,
        _ => throw new ArgumentOutOfRangeException(nameof(billingCycle))
    };
}
