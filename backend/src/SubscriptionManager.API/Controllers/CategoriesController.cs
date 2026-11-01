using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubscriptionManager.API.DTO;
using SubscriptionManager.API.Domain.Entities;
using SubscriptionManager.API.Infrastructure.Persistence;

namespace SubscriptionManager.API.Controllers;

/// <summary>
/// Manages subscription categories.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoriesController(SubscriptionManagerDbContext dbContext) : ControllerBase
{
    #region Get All

    /// <summary>
    /// Gets all categories.
    /// </summary>
    /// <returns>200 with the list of categories.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> Get()
    {
        var categories = await dbContext.Categories
            .Select(category => new CategoryResponse(category.Id, category.Name, category.CreatedAt))
            .ToListAsync();

        return Ok(categories);
    }

    #endregion

    #region Get By Id

    /// <summary>
    /// Gets a single category by id.
    /// </summary>
    /// <param name="id">Category id.</param>
    /// <returns>200 with the category, or 404 if it doesn't exist.</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetById(int id)
    {
        var category = await dbContext.Categories.FindAsync(id);

        if (category is null)
        {
            return NotFound();
        }

        return Ok(new CategoryResponse(category.Id, category.Name, category.CreatedAt));
    }

    #endregion

    #region Create

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="request">Category data.</param>
    /// <returns>201 with the created category, or 409 if the name is already in use.</returns>
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request)
    {
        var nameInUse = await dbContext.Categories.AnyAsync(c => c.Name == request.Name);

        if (nameInUse)
        {
            return Conflict($"A category named '{request.Name}' already exists.");
        }

        var category = new Category(request.Name);

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        var response = new CategoryResponse(category.Id, category.Name, category.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, response);
    }

    #endregion

    #region Update

    /// <summary>
    /// Renames an existing category.
    /// </summary>
    /// <param name="id">Category id.</param>
    /// <param name="request">New category data.</param>
    /// <returns>204 if updated, 404 if the category doesn't exist, or 409 if the name is already in use.</returns>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var category = await dbContext.Categories.FindAsync(id);

        if (category is null)
        {
            return NotFound();
        }

        var nameInUse = await dbContext.Categories.AnyAsync(c => c.Name == request.Name && c.Id != id);

        if (nameInUse)
        {
            return Conflict($"A category named '{request.Name}' already exists.");
        }

        category.Rename(request.Name);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Delete

    /// <summary>
    /// Deletes a category.
    /// </summary>
    /// <param name="id">Category id.</param>
    /// <returns>204 if deleted, or 404 if the category doesn't exist.</returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await dbContext.Categories.FindAsync(id);

        if (category is null)
        {
            return NotFound();
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    #endregion
}
