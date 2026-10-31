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

    private static void EnsureIsValid(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty.", nameof(name));
        }
    }

    private Category()
    {
        // Required by EF Core.
    }
}
