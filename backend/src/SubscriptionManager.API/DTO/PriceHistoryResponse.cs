namespace SubscriptionManager.API.DTO;

public record PriceHistoryResponse(
    int Id,
    int SubscriptionId,
    decimal OldPrice,
    decimal NewPrice,
    DateTime ChangedAt);
