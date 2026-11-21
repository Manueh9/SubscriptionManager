// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using System.Net;
using System.Net.Http.Json;
using SubscriptionManager.API.DTO;
using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.Tests;

public class CategoriesEndpointsTests(WebApiFactory factory) : IClassFixture<WebApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_ReturnsCreated_WithValidName()
    {
        var response = await _client.PostAsJsonAsync("/api/Categories", new CategoryRequest($"Streaming-{Guid.NewGuid()}"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenNameAlreadyExists()
    {
        var name = $"Fitness-{Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/api/Categories", new CategoryRequest(name));

        var response = await _client.PostAsJsonAsync("/api/Categories", new CategoryRequest(name));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenNameIsEmpty()
    {
        // Guards against regressing the fixed "known gap": this used to surface as a raw 500.
        var response = await _client.PostAsJsonAsync("/api/Categories", new CategoryRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsConflict_WhenCategoryHasSubscriptions()
    {
        // Guards against regressing the fixed "known gap": this used to surface as a raw 500.
        var categoryResponse = await _client.PostAsJsonAsync("/api/Categories", new CategoryRequest($"Category-{Guid.NewGuid()}"));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        await _client.PostAsJsonAsync("/api/Subscriptions", new SubscriptionCreateRequest(
            $"Netflix-{Guid.NewGuid()}", "Streaming service", 9.99m, BillingCycle.Monthly, DateTime.UtcNow, category!.Id));

        var response = await _client.DeleteAsync($"/api/Categories/{category.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
