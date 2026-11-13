namespace SubscriptionManager.API.DTO;

public record ExpensesSummaryResponse(decimal MonthlyTotal, decimal YearlyTotal, int ActiveSubscriptionCount);
