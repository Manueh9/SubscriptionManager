using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubscriptionManager.API.DTO;
using SubscriptionManager.API.Domain.Entities;
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
        await dbContext.SaveChangesAsync();

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
        await dbContext.SaveChangesAsync();

        return NoContent();
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
