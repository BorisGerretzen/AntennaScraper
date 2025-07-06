using System.Linq.Expressions;
using AntennaScraper.Lib.Entities;
using Microsoft.EntityFrameworkCore;

namespace AntennaScraper.Lib.Services.Sync.BaseSyncService;

internal interface IBaseSyncService
{
    /// <summary>
    ///     Syncs entities with the database.
    ///     Deletes entities that are not in the incoming collection, unless they match the additional delete condition.
    ///     Updates entities that are in the incoming collection, using the specified columns to update.
    ///     Creates new entities that are not in the database.
    /// </summary>
    /// <param name="entities">Entities to sync.</param>
    /// <param name="dbSet">DbSet of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="additionalDeleteCondition">
    ///     Additional condition database entries have to conform to in order to be deleted
    ///     if their ExternalId does not exist in incoming.
    /// </param>
    /// <param name="columnsToUpdate">Columns to update in case ExternalId already exists in db.</param>
    /// <typeparam name="T">Entity type.</typeparam>
    Task SyncObjectsAsync<T>(
        IEnumerable<T> entities,
        DbSet<T> dbSet,
        CancellationToken cancellationToken,
        Expression<Func<T, bool>>? additionalDeleteCondition = null,
        params Expression<Func<T, object?>>[] columnsToUpdate)
        where T : class, ISyncEntity;
}