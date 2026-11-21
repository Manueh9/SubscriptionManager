// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using System.Net.Http.Json;
using SubscriptionManager.API.DTO;
using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.Tests;

// Each test needs its own empty database, since the summary aggregates every active
// subscription — unlike the other endpoint tests, it can't tolerate sharing one instance
// of WebApiFactory (and its in-memory database) across test methods via IClassFixture.
public class ExpensesEndpointsTests : IDisposable
{
    private readonly WebApiFactory _factory = new();
    private readonly HttpClient _client;

    public ExpensesEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private async Task<int> CreateCategoryAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/Categories", new CategoryRequest($"Category-{Guid.NewGuid()}"));
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        return category!.Id;
    }

    private async Task<SubscriptionResponse> CreateSubscriptionAsync(int categoryId, decimal price, BillingCycle billingCycle)
    {
        var response = await _client.PostAsJsonAsync("/api/Subscriptions", new SubscriptionCreateRequest(
            $"Subscription-{Guid.NewGuid()}", "Description", price, billingCycle, DateTime.UtcNow, categoryId));

        return (await response.Content.ReadFromJsonAsync<SubscriptionResponse>())!;
    }

    [Fact]
    public async Task GetSummary_ReturnsZero_WhenNoActiveSubscriptions()
    {
        var summary = await _client.GetFromJsonAsync<ExpensesSummaryResponse>("/api/Expenses/summary");

        Assert.Equal(0m, summary!.MonthlyTotal);
        Assert.Equal(0m, summary.YearlyTotal);
        Assert.Equal(0, summary.ActiveSubscriptionCount);
    }

    [Fact]
    public async Task GetSummary_SumsMonthlyAndYearlySubscriptions_AndExcludesPaused()
    {
        var categoryId = await CreateCategoryAsync();
        await CreateSubscriptionAsync(categoryId, 10m, BillingCycle.Monthly); // 10/month, 120/year
        var toPause = await CreateSubscriptionAsync(categoryId, 120m, BillingCycle.Yearly); // 10/month, 120/year

        var summaryBeforePause = await _client.GetFromJsonAsync<ExpensesSummaryResponse>("/api/Expenses/summary");
        Assert.Equal(20m, summaryBeforePause!.MonthlyTotal);
        Assert.Equal(240m, summaryBeforePause.YearlyTotal);
        Assert.Equal(2, summaryBeforePause.ActiveSubscriptionCount);

        await _client.PutAsJsonAsync($"/api/Subscriptions/{toPause.Id}/status", new ChangeStatusRequest(SubscriptionStatus.Paused));

        var summaryAfterPause = await _client.GetFromJsonAsync<ExpensesSummaryResponse>("/api/Expenses/summary");
        Assert.Equal(10m, summaryAfterPause!.MonthlyTotal);
        Assert.Equal(120m, summaryAfterPause.YearlyTotal);
        Assert.Equal(1, summaryAfterPause.ActiveSubscriptionCount);
    }

    [Fact]
    public async Task GetSummary_ConvertsDailyBillingToYearlyAndMonthly()
    {
        var categoryId = await CreateCategoryAsync();
        await CreateSubscriptionAsync(categoryId, 1m, BillingCycle.Day);

        var summary = await _client.GetFromJsonAsync<ExpensesSummaryResponse>("/api/Expenses/summary");

        Assert.Equal(365m, summary!.YearlyTotal);
        Assert.Equal(30.42m, summary.MonthlyTotal);
    }
}
