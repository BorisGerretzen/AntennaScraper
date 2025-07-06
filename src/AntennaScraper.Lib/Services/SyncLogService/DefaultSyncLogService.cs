using AntennaScraper.Lib.Entities;
using AntennaScraper.Lib.Services.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace AntennaScraper.Lib.Services.SyncLogService;

public class DefaultSyncLogService(IUnitOfWork uow) : ISyncLogService
{
    public async Task LogSyncSuccessAsync(DateTime syncStart, CancellationToken cancellationToken)
    {
        await uow.ExecuteAsync(async (token, context) =>
        {
            var syncEnd = DateTime.UtcNow;
            var log = new SyncLog
            {
                SyncStartedAt = syncStart,
                SyncEndedAt = syncEnd,
                IsSuccessful = true
            };

            await context.SyncLogs.AddAsync(log, token);
            await context.SaveChangesAsync(token);
        }, cancellationToken);
    }
    
    public async Task LogSyncFailureAsync(DateTime syncStart, CancellationToken cancellationToken)
    {
        await uow.ExecuteAsync(async (token, context) =>
        {
            var syncEnd = DateTime.UtcNow;
            var log = new SyncLog
            {
                SyncStartedAt = syncStart,
                SyncEndedAt = syncEnd,
                IsSuccessful = false,
            };

            await context.SyncLogs.AddAsync(log, token);
            await context.SaveChangesAsync(token);
        }, cancellationToken);
    }

    public async Task<SyncLogDto?> GetLastSyncLogAsync(CancellationToken cancellationToken)
    {
        return await uow.ExecuteAsync(async (token, context) =>
        {
            var log = await context.SyncLogs
                .AsNoTracking()
                .OrderByDescending(log => log.SyncEndedAt)
                .FirstOrDefaultAsync(token);
            if (log == null) return null;
            
            return new SyncLogDto(log.SyncStartedAt, log.SyncEndedAt, log.IsSuccessful);
        }, cancellationToken);
    }
}