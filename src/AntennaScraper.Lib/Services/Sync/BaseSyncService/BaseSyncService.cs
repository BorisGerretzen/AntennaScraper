using System.Linq.Expressions;
using System.Reflection;
using AntennaScraper.Lib.Entities;
using Microsoft.EntityFrameworkCore;

namespace AntennaScraper.Lib.Services.Sync.BaseSyncService;

public record SyncResult(int Added, int Updated, int Deleted);

public class BaseSyncService : IBaseSyncService
{
    public async Task<SyncResult> SyncObjectsAsync<T>(
        IEnumerable<T> entities,
        DbSet<T> dbSet,
        CancellationToken cancellationToken,
        Expression<Func<T, bool>>? additionalDeleteCondition = null,
        params Expression<Func<T, object?>>[] columnsToUpdate) where T : class, ISyncEntity
    {
        var incoming = entities as IReadOnlyCollection<T> ?? entities.ToList();
        const int batchSize = 1000;

        var updateCols = columnsToUpdate
            .Select(c => (Name: GetPropertyName(c), Getter: c.Compile()))
            .ToArray();

        var newIds = incoming.Select(e => e.ExternalId).ToHashSet();

        var added = 0;
        var updated = 0;
        var deleted = 0;
        
        foreach (var batch in incoming.Chunk(batchSize))
        {
            var batchDistinct = batch.DistinctBy(e => e.ExternalId).ToList();
            var batchIds = batchDistinct.Select(e => e.ExternalId).ToHashSet();

            var existing = await dbSet
                .AsNoTracking()
                .Where(e => batchIds.Contains(e.ExternalId))
                .ToDictionaryAsync(e => e.ExternalId, cancellationToken);

            var newEntities = batchDistinct
                .Where(e => !existing.ContainsKey(e.ExternalId))
                .ToList();

            if (newEntities.Count != 0)
            {
                await dbSet.AddRangeAsync(newEntities, cancellationToken);
                added += newEntities.Count;
            }

            // Handle updates
            if (updateCols.Length > 0)
            {
                var updatedEntities = batchDistinct
                    .Where(e => existing.ContainsKey(e.ExternalId))
                    .ToList();

                if (updatedEntities.Count != 0)
                {
                    // Set the IDs and RowVersions from the existing entities
                    foreach (var entity in updatedEntities)
                    {
                        var dbEntity = existing[entity.ExternalId];
                        entity.Id = dbEntity.Id;
                        entity.RowVersion = dbEntity.RowVersion;
                    }
                    dbSet.AttachRange(updatedEntities);

                    foreach (var entity in updatedEntities)
                    {
                        var dbEntity = existing[entity.ExternalId];
                        var entry = dbSet.Entry(entity);

                        var anyChanged = false;

                        foreach (var col in updateCols)
                        {
                            var newVal = col.Getter(entity);
                            var oldVal = col.Getter(dbEntity);

                            if (!Equals(newVal, oldVal))
                            {
                                entry.Property(col.Name).IsModified = true;
                                anyChanged = true;
                            }
                        }

                        // Optional: if nothing changed, keep it totally untouched
                        if (!anyChanged)
                            entry.State = EntityState.Unchanged;
                        else
                            updated++;
                    }
                }
            }
        }

        // Delete entities that are not in the incoming collection
        var toDelete = dbSet.Where(e => !newIds.Contains(e.ExternalId));
        if (additionalDeleteCondition != null)
            toDelete = toDelete.Where(additionalDeleteCondition);
        deleted = await toDelete.CountAsync(cancellationToken);
        await toDelete.ExecuteDeleteAsync(cancellationToken);
        
        return new SyncResult(added, updated, deleted);
    }

    private static string GetPropertyName<T>(Expression<Func<T, object?>> expr)
    {
        var body = expr.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } u)
            body = u.Operand;

        if (body is MemberExpression { Member: PropertyInfo pi })
            return pi.Name;

        throw new ArgumentException(
            "columnsToUpdate must be a simple property accessor like x => x.SomeProperty",
            nameof(expr));
    }
}
