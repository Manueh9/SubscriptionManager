using System.ComponentModel.DataAnnotations;
using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.DTO;

public record SubscriptionCreateRequest(
    string Name,
    string Description,
    decimal Price,
    [EnumDataType(typeof(BillingCycle))] BillingCycle BillingCycle,
    DateTime StartDate,
    int CategoryId);
