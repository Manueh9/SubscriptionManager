using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubscriptionManager.API.DTO;
using SubscriptionManager.API.Domain.Entities;
using SubscriptionManager.API.Domain.Enum;
using SubscriptionManager.API.Infrastructure.Persistence;

namespace SubscriptionManager.API.Controllers;

/// <summary>
/// Manages subscriptions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController(SubscriptionManagerDbContext dbContext) : ControllerBase
{
    #region Get All

    /// <summary>
    /// Gets all subscriptions.
    /// </summary>
    /// <returns>200 with the list of subscriptions.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubscriptionResponse>>> Get()
    {
        var subscriptions = await dbContext.Subscriptions
            .Select(subscription => new SubscriptionResponse(
                subscription.Id,
                subscription.Name,
                subscription.Description,
                subscription.Price,
                subscription.BillingCycle,
                subscription.StartDate,
                subscription.EndDate,
                subscription.CreatedAt,
                subscription.UpdatedAt,
                subscription.Status,
                subscription.CategoryId))
            .ToListAsync();

        return Ok(subscriptions);
    }

    #endregion

    #region Get By Id

    /// <summary>
    /// Gets a single subscription by id.
    /// </summary>
    /// <param name="id">Subscription id.</param>
    /// <returns>200 with the subscription, or 404 if it doesn't exist.</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SubscriptionResponse>> GetById(int id)
    {
        var subscription = await dbContext.Subscriptions.FindAsync(id);

        if (subscription is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(subscription));
    }

    #endregion

    #region Upcoming Renewals

    /// <summary>
    /// Gets active subscriptions whose next renewal falls within the given window, ordered
    /// by date. Paused/cancelled subscriptions, and ones that have already ended, are
    /// excluded.
    /// </summary>
    /// <param name="withinDays">How many days ahead to look. Defaults to 30.</param>
    /// <returns>200 with the list of upcoming renewals.</returns>
    [HttpGet("upcoming")]
    public async Task<ActionResult<IEnumerable<UpcomingRenewalResponse>>> GetUpcoming([FromQuery] int withinDays = 30)
    {
        var today = DateTime.UtcNow.Date;
        var horizon = today.AddDays(withinDays);

        var subscriptions = await dbContext.Subscriptions
            .Where(subscription => subscription.Status == SubscriptionStatus.Active)
            .ToListAsync();

        var upcoming = subscriptions
            .Select(subscription => (subscription, nextRenewalDate: subscription.GetNextRenewalDate(today)))
            .Where(x => x.nextRenewalDate is not null && x.nextRenewalDate <= horizon)
            .OrderBy(x => x.nextRenewalDate)
            .Select(x => new UpcomingRenewalResponse(
                x.subscription.Id,
                x.subscription.Name,
                x.subscription.Price,
                x.subscription.BillingCycle,
                x.nextRenewalDate!.Value,
                x.subscription.CategoryId));

        return Ok(upcoming);
    }

    #endregion

    #region Create

    /// <summary>
    /// Creates a new subscription.
    /// </summary>
    /// <param name="request">Subscription data.</param>
    /// <returns>201 with the created subscription, 400 if the category doesn't exist, or 409 if the same name already exists in that category.</returns>
    [HttpPost]
    public async Task<ActionResult<SubscriptionResponse>> Create(SubscriptionCreateRequest request)
    {
        var category = await dbContext.Categories.FindAsync(request.CategoryId);

        if (category is null)
        {
            return BadRequest($"Category {request.CategoryId} does not exist.");
        }

        var nameInUse = await dbContext.Subscriptions
            .AnyAsync(s => s.Name == request.Name && s.CategoryId == request.CategoryId);

        if (nameInUse)
        {
            return Conflict($"A subscription named '{request.Name}' already exists in this category.");
        }

        var subscription = new Subscription(
            request.Name,
            request.Description,
            request.Price,
            request.BillingCycle,
            request.StartDate,
            category);

        dbContext.Subscriptions.Add(subscription);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict($"A subscription named '{request.Name}' already exists in this category.");
        }

        return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, ToResponse(subscription));
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates a subscription's details. Price is not editable here (see Price Histories).
    /// </summary>
    /// <param name="id">Subscription id.</param>
    /// <param name="request">New subscription data.</param>
    /// <returns>204 if updated, 404 if the subscription doesn't exist, 400 if the category doesn't exist, or 409 if the same name already exists in that category.</returns>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SubscriptionUpdateRequest request)
    {
        var subscription = await dbContext.Subscriptions.FindAsync(id);

        if (subscription is null)
        {
            return NotFound();
        }

        var category = await dbContext.Categories.FindAsync(request.CategoryId);

        if (category is null)
        {
            return BadRequest($"Category {request.CategoryId} does not exist.");
        }

        var nameInUse = await dbContext.Subscriptions
            .AnyAsync(s => s.Name == request.Name && s.CategoryId == request.CategoryId && s.Id != id);

        if (nameInUse)
        {
            return Conflict($"A subscription named '{request.Name}' already exists in this category.");
        }

        subscription.UpdateDetails(request.Name, request.Description, request.BillingCycle, request.StartDate, request.EndDate, category);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict($"A subscription named '{request.Name}' already exists in this category.");
        }

        return NoContent();
    }

    #endregion

    #region Change Price

    /// <summary>
    /// Changes a subscription's price and records the change in its price history.
    /// </summary>
    /// <param name="id">Subscription id.</param>
    /// <param name="request">The new price.</param>
    /// <returns>200 with the updated subscription, 404 if the subscription doesn't exist, or 400 if the new price is negative or equal to the current price.</returns>
    [HttpPut("{id:int}/price")]
    public async Task<ActionResult<SubscriptionResponse>> ChangePrice(int id, ChangePriceRequest request)
    {
        var subscription = await dbContext.Subscriptions.FindAsync(id);

        if (subscription is null)
        {
            return NotFound();
        }

        var priceHistory = subscription.ChangePrice(request.NewPrice);

        dbContext.PriceHistories.Add(priceHistory);
        await dbContext.SaveChangesAsync();

        return Ok(ToResponse(subscription));
    }

    #endregion

    #region Change Status

    /// <summary>
    /// Changes a subscription's status (Active/Paused/Cancelled).
    /// </summary>
    /// <param name="id">Subscription id.</param>
    /// <param name="request">The new status.</param>
    /// <returns>200 with the updated subscription, 404 if the subscription doesn't exist, or 400 if the new status equals the current one.</returns>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<SubscriptionResponse>> ChangeStatus(int id, ChangeStatusRequest request)
    {
        var subscription = await dbContext.Subscriptions.FindAsync(id);

        if (subscription is null)
        {
            return NotFound();
        }

        subscription.ChangeStatus(request.NewStatus);

        await dbContext.SaveChangesAsync();

        return Ok(ToResponse(subscription));
    }

    #endregion

    #region Delete

    /// <summary>
    /// Deletes a subscription.
    /// </summary>
    /// <param name="id">Subscription id.</param>
    /// <returns>204 if deleted, or 404 if the subscription doesn't exist.</returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var subscription = await dbContext.Subscriptions.FindAsync(id);

        if (subscription is null)
        {
            return NotFound();
        }

        dbContext.Subscriptions.Remove(subscription);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    private static SubscriptionResponse ToResponse(Subscription subscription) => new(
        subscription.Id,
        subscription.Name,
        subscription.Description,
        subscription.Price,
        subscription.BillingCycle,
        subscription.StartDate,
        subscription.EndDate,
        subscription.CreatedAt,
        subscription.UpdatedAt,
        subscription.Status,
        subscription.CategoryId);
}
