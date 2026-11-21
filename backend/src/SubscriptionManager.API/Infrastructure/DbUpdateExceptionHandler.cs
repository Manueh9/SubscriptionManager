// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubscriptionManager.API.Infrastructure.Persistence;

namespace SubscriptionManager.API.Infrastructure;

/// <summary>
/// Maps a database constraint violation (a unique index, or a foreign-key RESTRICT on delete)
/// to a 409 Conflict. Other causes of <see cref="DbUpdateException"/> (e.g. a transient
/// "database is locked" error under concurrent writes) are left unhandled, so they still
/// surface as a genuine unhandled 500 instead of being misreported as a 409 data conflict.
/// </summary>
public class DbUpdateExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException dbUpdateException || !dbUpdateException.IsConstraintViolation())
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
