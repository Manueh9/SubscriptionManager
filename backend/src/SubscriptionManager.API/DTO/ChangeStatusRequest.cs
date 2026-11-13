using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.DTO;

public record ChangeStatusRequest(SubscriptionStatus NewStatus);
