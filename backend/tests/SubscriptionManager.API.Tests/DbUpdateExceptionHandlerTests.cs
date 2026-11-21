// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SubscriptionManager.API.Infrastructure;

namespace SubscriptionManager.API.Tests;

/// <summary>
/// Pure unit tests of <see cref="DbUpdateExceptionHandler"/> — no HTTP server, no database,
/// just the handler's own decision logic given a crafted exception.
/// </summary>
public class DbUpdateExceptionHandlerTests
{
    private readonly DbUpdateExceptionHandler _handler = new();

    private static DefaultHttpContext NewHttpContext() => new() { Response = { Body = new MemoryStream() } };

    [Fact]
    public async Task TryHandleAsync_HandlesIt_WhenInnerExceptionIsASqliteConstraintViolation()
    {
        var sqliteException = new SqliteException("UNIQUE constraint failed", 19);
        var exception = new DbUpdateException("Saving changes failed.", sqliteException);
        var httpContext = NewHttpContext();

        var handled = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_DoesNotHandleIt_WhenInnerExceptionIsATransientSqliteError()
    {
        // SQLITE_BUSY (5): the database is locked by another connection - a transient
        // infrastructure failure, not a data conflict. Must NOT be reported as a 409.
        var sqliteException = new SqliteException("database is locked", 5);
        var exception = new DbUpdateException("Saving changes failed.", sqliteException);
        var httpContext = NewHttpContext();

        var handled = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.False(handled);
    }

    [Fact]
    public async Task TryHandleAsync_DoesNotHandleIt_WhenThereIsNoInnerException()
    {
        var exception = new DbUpdateException("Saving changes failed.");
        var httpContext = NewHttpContext();

        var handled = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.False(handled);
    }

    [Fact]
    public async Task TryHandleAsync_DoesNotHandleIt_WhenTheInnerExceptionIsNotASqliteException()
    {
        var exception = new DbUpdateException("Saving changes failed.", new InvalidOperationException("Something else failed."));
        var httpContext = NewHttpContext();

        var handled = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.False(handled);
    }

    [Fact]
    public async Task TryHandleAsync_DoesNotHandleIt_WhenTheExceptionIsNotADbUpdateException()
    {
        var exception = new InvalidOperationException("Not a database error at all.");
        var httpContext = NewHttpContext();

        var handled = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.False(handled);
    }
}
