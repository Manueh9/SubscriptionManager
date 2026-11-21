// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.Domain.Entities;

public class Subscription
{
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public decimal Price { get; private set; }
    public BillingCycle BillingCycle { get; private set; }

    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    public Subscription(string name, string description, decimal price, BillingCycle billingCycle, DateTime startDate, Category category)
    {
        EnsureIsValid(name, price);

        Name = name;
        Description = description;
        Price = price;
        BillingCycle = billingCycle;
        StartDate = startDate;
        Category = category;
        CategoryId = category.Id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Status = SubscriptionStatus.Active;
    }

    public PriceHistory ChangePrice(decimal newPrice)
    {
        var priceHistory = new PriceHistory(this, Price, newPrice);

        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;

        return priceHistory;
    }

    public void ChangeStatus(SubscriptionStatus newStatus)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(newStatus, Status);

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Computes the date of the next renewal on or after <paramref name="asOf"/> (compared
    /// by calendar date, time-of-day ignored). Returns null if the subscription isn't
    /// Active, or if that date would fall after <see cref="EndDate"/>.
    /// </summary>
    public DateTime? GetNextRenewalDate(DateTime asOf)
    {
        if (Status != SubscriptionStatus.Active)
        {
            return null;
        }

        var today = asOf.Date;
        var start = StartDate.Date;

        var next = start >= today
            ? start
            : BillingCycle switch
            {
                BillingCycle.Day => today,
                BillingCycle.Monthly => NextMonthlyOccurrence(start, today, monthsPerCycle: 1),
                BillingCycle.Yearly => NextMonthlyOccurrence(start, today, monthsPerCycle: 12),
                _ => throw new ArgumentOutOfRangeException(nameof(BillingCycle))
            };

        if (EndDate is not null && next > EndDate.Value.Date)
        {
            return null;
        }

        return next;

        // Always recomputes from the original `start` (never from a previously-clipped
        // intermediate date), so a short month (e.g. a 31st or a Feb 29th) only gets clipped
        // by AddMonths once per call instead of compounding across every step after it.
        static DateTime NextMonthlyOccurrence(DateTime start, DateTime today, int monthsPerCycle)
        {
            var cyclesElapsed = (((today.Year - start.Year) * 12) + today.Month - start.Month) / monthsPerCycle;
            var next = start.AddMonths(cyclesElapsed * monthsPerCycle);

            return next < today ? start.AddMonths((cyclesElapsed + 1) * monthsPerCycle) : next;
        }
    }

    public void UpdateDetails(string name, string description, BillingCycle billingCycle, DateTime startDate, DateTime? endDate, Category category)
    {
        EnsureIsValid(name, Price);

        Name = name;
        Description = description;
        BillingCycle = billingCycle;
        StartDate = startDate;
        EndDate = endDate;
        Category = category;
        CategoryId = category.Id;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void EnsureIsValid(string name, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(price);
    }

    private Subscription()
    {
        // Required by EF Core.
    }
}
