using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SubscriptionManager.API.Infrastructure;

/// <summary>
/// Maps domain guard-clause failures (<see cref="ArgumentException"/> and its subclasses,
/// e.g. <see cref="ArgumentOutOfRangeException"/>) to a proper 400 Bad Request instead of
/// letting them surface as an unhandled 500.
/// </summary>
public class ArgumentExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ArgumentException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request.",
            Detail = exception.Message
        }, cancellationToken);

        return true;
    }
}
