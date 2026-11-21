// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using System.Net.Http.Json;
using SubscriptionManager.API.DTO;
using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.Tests;

public class PriceHistoriesEndpointsTests(WebApiFactory factory) : IClassFixture<WebApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_ReturnsRecordedEntry_AfterPriceChange()
    {
        var categoryResponse = await _client.PostAsJsonAsync("/api/Categories", new CategoryRequest($"Category-{Guid.NewGuid()}"));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var subscriptionResponse = await _client.PostAsJsonAsync("/api/Subscriptions", new SubscriptionCreateRequest(
            $"Netflix-{Guid.NewGuid()}", "Streaming service", 9.99m, BillingCycle.Monthly, DateTime.UtcNow, category!.Id));
        var subscription = await subscriptionResponse.Content.ReadFromJsonAsync<SubscriptionResponse>();

        await _client.PutAsJsonAsync($"/api/Subscriptions/{subscription!.Id}/price", new ChangePriceRequest(14.99m));

        var priceHistories = await _client.GetFromJsonAsync<List<PriceHistoryResponse>>(
            $"/api/PriceHistories?subscriptionId={subscription.Id}");

        var entry = Assert.Single(priceHistories!);
        Assert.Equal(9.99m, entry.OldPrice);
        Assert.Equal(14.99m, entry.NewPrice);
    }
}
