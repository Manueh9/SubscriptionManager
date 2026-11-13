using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SubscriptionManager.API.Infrastructure;

/// <summary>
/// Maps an unhandled <see cref="DbUpdateException"/> (e.g. a foreign-key RESTRICT violation
/// on delete, or a unique-constraint race not already caught by the controller) to a 409
/// Conflict instead of letting it surface as an unhandled 500.
/// </summary>
public class DbUpdateExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict.",
            Detail = "The request conflicts with the current state of the data (e.g. a record still referenced by another, or a duplicate)."
        }, cancellationToken);

        return true;
    }
}
