using System.Net;
using System.Net.Http.Json;
using SubscriptionManager.API.DTO;
using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.Tests;

public class SubscriptionsEndpointsTests(WebApiFactory factory) : IClassFixture<WebApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<int> CreateCategoryAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/Categories", new CategoryRequest($"Category-{Guid.NewGuid()}"));
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        return category!.Id;
    }

    private async Task<SubscriptionResponse> CreateSubscriptionAsync(int categoryId, decimal price = 9.99m)
    {
        var response = await _client.PostAsJsonAsync("/api/Subscriptions", new SubscriptionCreateRequest(
            $"Netflix-{Guid.NewGuid()}", "Streaming service", price, BillingCycle.Monthly, DateTime.UtcNow, categoryId));

        return (await response.Content.ReadFromJsonAsync<SubscriptionResponse>())!;
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithValidData()
    {
        var categoryId = await CreateCategoryAsync();

        var response = await _client.PostAsJsonAsync("/api/Subscriptions", new SubscriptionCreateRequest(
            $"Spotify-{Guid.NewGuid()}", "Music", 9.99m, BillingCycle.Monthly, DateTime.UtcNow, categoryId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenCategoryDoesNotExist()
    {
        var response = await _client.PostAsJsonAsync("/api/Subscriptions", new SubscriptionCreateRequest(
            $"Spotify-{Guid.NewGuid()}", "Music", 9.99m, BillingCycle.Monthly, DateTime.UtcNow, 999_999));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePrice_ReturnsOk_AndUpdatesPrice()
    {
        var categoryId = await CreateCategoryAsync();
        var subscription = await CreateSubscriptionAsync(categoryId, price: 9.99m);

        var response = await _client.PutAsJsonAsync($"/api/Subscriptions/{subscription.Id}/price", new ChangePriceRequest(14.99m));
        var updated = await response.Content.ReadFromJsonAsync<SubscriptionResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(14.99m, updated!.Price);
    }

    [Fact]
    public async Task ChangePrice_ReturnsBadRequest_WhenNewPriceEqualsCurrentPrice()
    {
        // Guards against regressing the fixed "known gap": this used to surface as a raw 500.
        var categoryId = await CreateCategoryAsync();
        var subscription = await CreateSubscriptionAsync(categoryId, price: 9.99m);

        var response = await _client.PutAsJsonAsync($"/api/Subscriptions/{subscription.Id}/price", new ChangePriceRequest(9.99m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePrice_ReturnsNotFound_WhenSubscriptionDoesNotExist()
    {
        var response = await _client.PutAsJsonAsync("/api/Subscriptions/999999/price", new ChangePriceRequest(14.99m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
