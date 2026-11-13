using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.DTO;

public record UpcomingRenewalResponse(
    int SubscriptionId,
    string Name,
    decimal Price,
    BillingCycle BillingCycle,
    DateTime NextRenewalDate,
    int CategoryId);
