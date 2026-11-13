using System.Net.Http.Json;
using SubscriptionManager.API.DTO;
using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.Tests;

// Isolated per test like ExpensesEndpointsTests: /upcoming aggregates every active
// subscription in the database, so it can't tolerate sharing one WebApiFactory instance
// (and its in-memory database) across test methods via IClassFixture.
public class UpcomingRenewalsEndpointsTests : IDisposable
{
    private readonly WebApiFactory _factory = new();
    private readonly HttpClient _client;

    public UpcomingRenewalsEndpointsTests()
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

    private async Task<SubscriptionResponse> CreateSubscriptionAsync(int categoryId, DateTime startDate, BillingCycle billingCycle)
    {
        var response = await _client.PostAsJsonAsync("/api/Subscriptions", new SubscriptionCreateRequest(
            $"Subscription-{Guid.NewGuid()}", "Description", 9.99m, billingCycle, startDate, categoryId));

        return (await response.Content.ReadFromJsonAsync<SubscriptionResponse>())!;
    }

    [Fact]
    public async Task GetUpcoming_IncludesSubscription_WhenNextRenewalIsWithinDefaultWindow()
    {
        var categoryId = await CreateCategoryAsync();
        var subscription = await CreateSubscriptionAsync(categoryId, DateTime.UtcNow.Date, BillingCycle.Monthly);

        var upcoming = await _client.GetFromJsonAsync<List<UpcomingRenewalResponse>>("/api/Subscriptions/upcoming");

        Assert.Contains(upcoming!, renewal => renewal.SubscriptionId == subscription.Id);
    }

    [Fact]
    public async Task GetUpcoming_ExcludesSubscription_WhenNextRenewalIsBeyondTheWindow()
    {
        var categoryId = await CreateCategoryAsync();
        var subscription = await CreateSubscriptionAsync(categoryId, DateTime.UtcNow.Date.AddDays(-1), BillingCycle.Yearly);

        var upcoming = await _client.GetFromJsonAsync<List<UpcomingRenewalResponse>>("/api/Subscriptions/upcoming");

        Assert.DoesNotContain(upcoming!, renewal => renewal.SubscriptionId == subscription.Id);
    }

    [Fact]
    public async Task GetUpcoming_ExcludesPausedSubscriptions()
    {
        var categoryId = await CreateCategoryAsync();
        var subscription = await CreateSubscriptionAsync(categoryId, DateTime.UtcNow.Date, BillingCycle.Monthly);

        await _client.PutAsJsonAsync($"/api/Subscriptions/{subscription.Id}/status", new ChangeStatusRequest(SubscriptionStatus.Paused));

        var upcoming = await _client.GetFromJsonAsync<List<UpcomingRenewalResponse>>("/api/Subscriptions/upcoming");

        Assert.DoesNotContain(upcoming!, renewal => renewal.SubscriptionId == subscription.Id);
    }

    [Fact]
    public async Task GetUpcoming_ExcludesSubscriptions_WhoseEndDateHasAlreadyPassed()
    {
        var categoryId = await CreateCategoryAsync();
        var subscription = await CreateSubscriptionAsync(categoryId, DateTime.UtcNow.Date, BillingCycle.Monthly);

        await _client.PutAsJsonAsync($"/api/Subscriptions/{subscription.Id}", new SubscriptionUpdateRequest(
            subscription.Name,
            subscription.Description,
            subscription.BillingCycle,
            subscription.StartDate,
            DateTime.UtcNow.Date.AddDays(-1),
            categoryId));

        var upcoming = await _client.GetFromJsonAsync<List<UpcomingRenewalResponse>>("/api/Subscriptions/upcoming");

        Assert.DoesNotContain(upcoming!, renewal => renewal.SubscriptionId == subscription.Id);
    }

    [Fact]
    public async Task GetUpcoming_RespectsWithinDaysQueryParameter()
    {
        var categoryId = await CreateCategoryAsync();
        var subscription = await CreateSubscriptionAsync(categoryId, DateTime.UtcNow.Date.AddDays(10), BillingCycle.Monthly);

        var narrowWindow = await _client.GetFromJsonAsync<List<UpcomingRenewalResponse>>("/api/Subscriptions/upcoming?withinDays=5");
        var wideWindow = await _client.GetFromJsonAsync<List<UpcomingRenewalResponse>>("/api/Subscriptions/upcoming?withinDays=15");

        Assert.DoesNotContain(narrowWindow!, renewal => renewal.SubscriptionId == subscription.Id);
        Assert.Contains(wideWindow!, renewal => renewal.SubscriptionId == subscription.Id);
    }
}
