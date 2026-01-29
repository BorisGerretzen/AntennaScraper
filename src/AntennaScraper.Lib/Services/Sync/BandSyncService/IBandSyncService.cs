using AntennaScraper.Lib.Services.Data.BandService;
using AntennaScraper.Lib.Services.Sync.BaseSyncService;

namespace AntennaScraper.Lib.Services.Sync.BandSyncService;

public interface IBandSyncService
{
    Task<SyncResult> SyncBandsAsync(IEnumerable<BandDto> bands, CancellationToken cancellationToken = default);
}