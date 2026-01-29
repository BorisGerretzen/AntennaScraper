using AntennaScraper.Lib.Services.Data.ProviderService;
using AntennaScraper.Lib.Services.Sync.BaseSyncService;

namespace AntennaScraper.Lib.Services.Sync.ProviderSyncService;

public interface IProviderSyncService
{
    Task<SyncResult> SyncProvidersAsync(IEnumerable<ProviderDto> providers, CancellationToken cancellationToken);
}