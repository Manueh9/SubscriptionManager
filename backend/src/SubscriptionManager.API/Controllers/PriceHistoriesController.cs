using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubscriptionManager.API.DTO;
using SubscriptionManager.API.Infrastructure.Persistence;

namespace SubscriptionManager.API.Controllers;

/// <summary>
/// Reads subscription price change history. Records are created automatically when a
/// subscription's price changes, so there is no create/update/delete here.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PriceHistoriesController(SubscriptionManagerDbContext dbContext) : ControllerBase
{
    #region Get All

    /// <summary>
    /// Gets price history records, optionally filtered by subscription.
    /// </summary>
    /// <param name="subscriptionId">Optional subscription id to filter by.</param>
    /// <returns>200 with the list of price history records.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PriceHistoryResponse>>> Get([FromQuery] int? subscriptionId)
    {
        var query = dbContext.PriceHistories.AsQueryable();

        if (subscriptionId is not null)
        {
            query = query.Where(priceHistory => priceHistory.SubscriptionId == subscriptionId);
        }

        var priceHistories = await query
            .Select(priceHistory => new PriceHistoryResponse(
                priceHistory.Id,
                priceHistory.SubscriptionId,
                priceHistory.OldPrice,
                priceHistory.NewPrice,
                priceHistory.ChangedAt))
            .ToListAsync();

        return Ok(priceHistories);
    }

    #endregion

    #region Get By Id

    /// <summary>
    /// Gets a single price history record by id.
    /// </summary>
    /// <param name="id">Price history record id.</param>
    /// <returns>200 with the record, or 404 if it doesn't exist.</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PriceHistoryResponse>> GetById(int id)
    {
        var priceHistory = await dbContext.PriceHistories.FindAsync(id);

        if (priceHistory is null)
        {
            return NotFound();
        }

        return Ok(new PriceHistoryResponse(
            priceHistory.Id,
            priceHistory.SubscriptionId,
            priceHistory.OldPrice,
            priceHistory.NewPrice,
            priceHistory.ChangedAt));
    }

    #endregion
}
