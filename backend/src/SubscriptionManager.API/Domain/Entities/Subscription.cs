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
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Subscription name cannot be empty.", nameof(name));
        }

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }
    }

    private Subscription()
    {
        // Required by EF Core.
    }
}
