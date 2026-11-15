using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace SubscriptionManager.API.Infrastructure.Persistence;

public static class DbUpdateExceptionExtensions
{
    private const int SqliteConstraintViolation = 19; // SQLITE_CONSTRAINT

    /// <summary>
    /// Whether the save failed because the data violates a database constraint (a unique index,
    /// or a foreign-key RESTRICT on delete) rather than for some other reason. A
    /// <see cref="DbUpdateException"/> can also wrap a transient failure (e.g. SQLite's
    /// "database is locked" under concurrent writes), which is a server-side hiccup worth
    /// retrying — not a conflict with the data the client sent.
    /// </summary>
    public static bool IsConstraintViolation(this DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintViolation };
}
