using AntennaScraper.Lib.Services.Data.ProviderService;

namespace AntennaScraper.Lib.Services.Sync.ProviderSyncService;

public interface IProviderSyncService
{
    Task SyncProvidersAsync(IEnumerable<ProviderDto> providers, CancellationToken cancellationToken);
}