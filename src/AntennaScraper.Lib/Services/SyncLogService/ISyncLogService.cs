namespace AntennaScraper.Lib.Services.SyncLogService;

public interface ISyncLogService
{
    Task LogSyncSuccessAsync(DateTime syncStart, CancellationToken cancellationToken);
    Task LogSyncFailureAsync(DateTime syncStart, CancellationToken cancellationToken);
    Task<SyncLogDto?> GetLastSyncLogAsync(CancellationToken cancellationToken);
}