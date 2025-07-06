using System.Linq.Expressions;
using AntennaScraper.Lib.Entities;
using Microsoft.EntityFrameworkCore;

namespace AntennaScraper.Lib.Services.Sync.BaseSyncService;

public class BaseSyncService : IBaseSyncService
{
    public async Task SyncObjectsAsync<T>(
        IEnumerable<T> entities,
        DbSet<T> dbSet,
        CancellationToken cancellationToken,
        Expression<Func<T, bool>>? additionalDeleteCondition = null,
        params Expression<Func<T, object?>>[] columnsToUpdate) where T : class, ISyncEntity
    {
        var incoming = entities as IReadOnlyCollection<T> ?? entities.ToList();
        const int batchSize = 1000;
        var newIds = incoming
            .Select(e => e.ExternalId)
            .ToHashSet();

        foreach (var batch in incoming.Chunk(batchSize))
        {
            var batchIds = batch
                .Select(e => e.ExternalId)
                .ToHashSet();

            var metaData = await dbSet
                .AsNoTracking()
                .Where(e => batchIds.Contains(e.ExternalId))
                .Select(e => new { e.ExternalId, e.Id, e.RowVersion })
                .ToDictionaryAsync(e => e.ExternalId, e => (e.Id, e.RowVersion), cancellationToken);
            var existingIds = metaData.Keys.ToHashSet();

            var newEntities = batch
                .DistinctBy(e => e.ExternalId)
                .Where(e => !existingIds.Contains(e.ExternalId))
                .ToList();

            if (newEntities.Count != 0) await dbSet.AddRangeAsync(newEntities, cancellationToken);

            var updatedEntities = batch
                .Where(e => existingIds.Contains(e.ExternalId))
                .ToList();
            if (updatedEntities.Count != 0 && columnsToUpdate.Length > 0)
            {
                foreach (var entity in updatedEntities)
                    if (metaData.TryGetValue(entity.ExternalId, out var id))
                    {
                        entity.Id = id.Id;
                        entity.RowVersion = id.RowVersion;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Entity with ExternalId {entity.ExternalId} not found in the database.");
                    }

                dbSet.AttachRange(updatedEntities);
                foreach (var entity in updatedEntities)
                foreach (var column in columnsToUpdate)
                    dbSet.Entry(entity).Property(column).IsModified = true;
            }
        }

        // Delete entities that are not in the incoming collection
        var toDelete = dbSet.Where(e => !newIds.Contains(e.ExternalId));
        if (additionalDeleteCondition != null) toDelete = toDelete.Where(additionalDeleteCondition);
        await toDelete
            .ExecuteDeleteAsync(cancellationToken);
    }
}