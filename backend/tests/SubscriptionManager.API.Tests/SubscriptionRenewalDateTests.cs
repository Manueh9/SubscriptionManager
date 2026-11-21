// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using SubscriptionManager.API.Domain.Entities;
using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.Tests;

/// <summary>
/// Pure unit tests of <see cref="Subscription.GetNextRenewalDate"/> — no HTTP server, no
/// database. Exhaustively covers every branch: subscription status, a future start date,
/// each billing cycle, the day-31/Feb-29 clipping edge case this logic was fixed for, and
/// the EndDate cap.
/// </summary>
public class SubscriptionRenewalDateTests
{
    private static Subscription NewSubscription(DateTime startDate, BillingCycle billingCycle, decimal price = 9.99m) =>
        new($"Subscription-{Guid.NewGuid()}", "Description", price, billingCycle, startDate, new Category($"Category-{Guid.NewGuid()}"));

    // --- Status ---

    [Fact]
    public void GetNextRenewalDate_ReturnsNull_WhenPaused()
    {
        var subscription = NewSubscription(new DateTime(2026, 1, 1), BillingCycle.Monthly);
        subscription.ChangeStatus(SubscriptionStatus.Paused);

        Assert.Null(subscription.GetNextRenewalDate(new DateTime(2026, 2, 1)));
    }

    [Fact]
    public void GetNextRenewalDate_ReturnsNull_WhenCancelled()
    {
        var subscription = NewSubscription(new DateTime(2026, 1, 1), BillingCycle.Monthly);
        subscription.ChangeStatus(SubscriptionStatus.Cancelled);

        Assert.Null(subscription.GetNextRenewalDate(new DateTime(2026, 2, 1)));
    }

    // --- Start date in the future (or exactly today): the first renewal IS the start date ---

    [Fact]
    public void GetNextRenewalDate_ReturnsStartDate_WhenStartDateIsInTheFuture()
    {
        var subscription = NewSubscription(new DateTime(2026, 6, 15), BillingCycle.Yearly);

        Assert.Equal(new DateTime(2026, 6, 15), subscription.GetNextRenewalDate(new DateTime(2026, 1, 1)));
    }

    [Fact]
    public void GetNextRenewalDate_ReturnsStartDate_WhenStartDateEqualsToday()
    {
        var subscription = NewSubscription(new DateTime(2026, 6, 15), BillingCycle.Day);

        Assert.Equal(new DateTime(2026, 6, 15), subscription.GetNextRenewalDate(new DateTime(2026, 6, 15)));
    }

    // --- Day cycle: every day is an occurrence, so once started the next one is always today ---

    [Fact]
    public void GetNextRenewalDate_ReturnsToday_ForDayCycle_WhenStartedLongAgo()
    {
        var subscription = NewSubscription(new DateTime(2020, 1, 1), BillingCycle.Day);

        Assert.Equal(new DateTime(2026, 11, 13), subscription.GetNextRenewalDate(new DateTime(2026, 11, 13)));
    }

    [Fact]
    public void GetNextRenewalDate_ReturnsToday_ForDayCycle_WhenStartedYesterday()
    {
        var subscription = NewSubscription(new DateTime(2026, 11, 12), BillingCycle.Day);

        Assert.Equal(new DateTime(2026, 11, 13), subscription.GetNextRenewalDate(new DateTime(2026, 11, 13)));
    }

    // --- Monthly cycle, no short-month involved ---

    [Fact]
    public void GetNextRenewalDate_StepsForwardByMonth_UntilOnOrAfterToday()
    {
        var subscription = NewSubscription(new DateTime(2026, 1, 10), BillingCycle.Monthly);

        Assert.Equal(new DateTime(2026, 3, 10), subscription.GetNextRenewalDate(new DateTime(2026, 3, 5)));
    }

    [Fact]
    public void GetNextRenewalDate_ReturnsToday_WhenTodayIsExactlyAMonthlyOccurrence()
    {
        var subscription = NewSubscription(new DateTime(2026, 1, 10), BillingCycle.Monthly);

        Assert.Equal(new DateTime(2026, 3, 10), subscription.GetNextRenewalDate(new DateTime(2026, 3, 10)));
    }

    // --- Monthly cycle, the day-31 clipping bug this method was fixed for ---

    [Fact]
    public void GetNextRenewalDate_DoesNotCompoundTheClip_ForADay31StartDate()
    {
        // Jan 31 -> Feb (clipped to 28) -> the bug stepped from the clipped 28, landing on
        // Apr 28 instead of the correct Apr 30. This must now be computed fresh from Jan 31
        // every time, giving the correct Apr 30.
        var subscription = NewSubscription(new DateTime(2026, 1, 31), BillingCycle.Monthly);

        Assert.Equal(new DateTime(2026, 4, 30), subscription.GetNextRenewalDate(new DateTime(2026, 4, 15)));
    }

    [Fact]
    public void GetNextRenewalDate_DoesNotDriftAcrossAFullYear_ForADay31StartDate()
    {
        var subscription = NewSubscription(new DateTime(2026, 1, 31), BillingCycle.Monthly);

        Assert.Equal(new DateTime(2026, 12, 31), subscription.GetNextRenewalDate(new DateTime(2026, 12, 1)));
    }

    [Fact]
    public void GetNextRenewalDate_HandlesADay31Occurrence_ThatHasAlreadyPassed()
    {
        // Jan 31, 2026 -> ... -> Jan 31, 2027 (both already passed by the asOf date below) ->
        // next is Feb 28, 2027 (clipped once, directly from Jan 31, 2027 - not compounded).
        var subscription = NewSubscription(new DateTime(2026, 1, 31), BillingCycle.Monthly);

        Assert.Equal(new DateTime(2027, 2, 28), subscription.GetNextRenewalDate(new DateTime(2027, 2, 15)));
    }

    // --- Yearly cycle, no leap-day involved ---

    [Fact]
    public void GetNextRenewalDate_StepsForwardByYear_WhenThisYearsOccurrenceHasNotHappenedYet()
    {
        var subscription = NewSubscription(new DateTime(2020, 5, 1), BillingCycle.Yearly);

        Assert.Equal(new DateTime(2026, 5, 1), subscription.GetNextRenewalDate(new DateTime(2026, 4, 1)));
    }

    [Fact]
    public void GetNextRenewalDate_StepsForwardByYear_WhenThisYearsOccurrenceAlreadyPassed()
    {
        var subscription = NewSubscription(new DateTime(2020, 5, 1), BillingCycle.Yearly);

        Assert.Equal(new DateTime(2027, 5, 1), subscription.GetNextRenewalDate(new DateTime(2026, 6, 1)));
    }

    // --- Yearly cycle, Feb 29 start date (the same clipping concern, one year at a time) ---

    [Fact]
    public void GetNextRenewalDate_ClipsFeb29OnlyOnce_WhenThisYearsOccurrenceHasNotHappenedYet()
    {
        var subscription = NewSubscription(new DateTime(2024, 2, 29), BillingCycle.Yearly);

        Assert.Equal(new DateTime(2025, 2, 28), subscription.GetNextRenewalDate(new DateTime(2025, 2, 1)));
    }

    [Fact]
    public void GetNextRenewalDate_ClipsFeb29OnlyOnce_WhenThisYearsOccurrenceAlreadyPassed()
    {
        var subscription = NewSubscription(new DateTime(2024, 2, 29), BillingCycle.Yearly);

        Assert.Equal(new DateTime(2026, 2, 28), subscription.GetNextRenewalDate(new DateTime(2025, 3, 15)));
    }

    // --- EndDate cap ---

    [Fact]
    public void GetNextRenewalDate_ReturnsTheDate_WhenEndDateIsExactlyThatDate()
    {
        var subscription = NewSubscription(new DateTime(2026, 1, 10), BillingCycle.Monthly);
        subscription.UpdateDetails("Name", "Description", BillingCycle.Monthly, new DateTime(2026, 1, 10), new DateTime(2026, 3, 10), subscription.Category);

        Assert.Equal(new DateTime(2026, 3, 10), subscription.GetNextRenewalDate(new DateTime(2026, 3, 5)));
    }

    [Fact]
    public void GetNextRenewalDate_ReturnsNull_WhenEndDateIsBeforeTheComputedDate()
    {
        var subscription = NewSubscription(new DateTime(2026, 1, 10), BillingCycle.Monthly);
        subscription.UpdateDetails("Name", "Description", BillingCycle.Monthly, new DateTime(2026, 1, 10), new DateTime(2026, 3, 9), subscription.Category);

        Assert.Null(subscription.GetNextRenewalDate(new DateTime(2026, 3, 5)));
    }

    [Fact]
    public void GetNextRenewalDate_ReturnsTheDate_WhenEndDateIsAfterTheComputedDate()
    {
        var subscription = NewSubscription(new DateTime(2026, 1, 10), BillingCycle.Monthly);
        subscription.UpdateDetails("Name", "Description", BillingCycle.Monthly, new DateTime(2026, 1, 10), new DateTime(2026, 12, 31), subscription.Category);

        Assert.Equal(new DateTime(2026, 3, 10), subscription.GetNextRenewalDate(new DateTime(2026, 3, 5)));
    }

    [Fact]
    public void GetNextRenewalDate_ReturnsNull_ForDayCycle_WhenEndDateHasAlreadyPassed()
    {
        var subscription = NewSubscription(new DateTime(2026, 1, 1), BillingCycle.Day);
        subscription.UpdateDetails("Name", "Description", BillingCycle.Day, new DateTime(2026, 1, 1), new DateTime(2026, 11, 1), subscription.Category);

        Assert.Null(subscription.GetNextRenewalDate(new DateTime(2026, 11, 13)));
    }

    // --- Undefined billing cycle (unreachable through the API, which validates it, but the
    // domain method should still fail loudly rather than silently returning a wrong date) ---

    [Fact]
    public void GetNextRenewalDate_Throws_WhenBillingCycleIsNotDefined()
    {
        var subscription = NewSubscription(new DateTime(2026, 1, 1), (BillingCycle)999);

        Assert.Throws<ArgumentOutOfRangeException>(() => subscription.GetNextRenewalDate(new DateTime(2026, 2, 1)));
    }
}
