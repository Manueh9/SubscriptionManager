using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.DTO;

public record SubscriptionResponse(
    int Id,
    string Name,
    string Description,
    decimal Price,
    BillingCycle BillingCycle,
    DateTime StartDate,
    DateTime? EndDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SubscriptionStatus Status,
    int CategoryId);
