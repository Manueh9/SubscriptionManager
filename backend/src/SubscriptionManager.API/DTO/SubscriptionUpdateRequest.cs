using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.DTO;

public record SubscriptionUpdateRequest(
    string Name,
    string Description,
    BillingCycle BillingCycle,
    DateTime StartDate,
    DateTime? EndDate,
    int CategoryId);
