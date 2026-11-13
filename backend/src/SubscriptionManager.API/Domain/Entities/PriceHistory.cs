namespace SubscriptionManager.API.Domain.Entities;

public class PriceHistory
{
    public int Id { get; private set; }

    public int SubscriptionId { get; private set; }
    public Subscription Subscription { get; private set; } = null!;

    public decimal OldPrice { get; private set; }
    public decimal NewPrice { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public PriceHistory(Subscription subscription, decimal oldPrice, decimal newPrice)
    {
        EnsureIsValid(oldPrice, newPrice);

        Subscription = subscription;
        SubscriptionId = subscription.Id;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        ChangedAt = DateTime.UtcNow;
    }

    private static void EnsureIsValid(decimal oldPrice, decimal newPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(oldPrice);
        ArgumentOutOfRangeException.ThrowIfNegative(newPrice);
        ArgumentOutOfRangeException.ThrowIfEqual(newPrice, oldPrice);
    }

    private PriceHistory()
    {
        // Required by EF Core.
    }
}
