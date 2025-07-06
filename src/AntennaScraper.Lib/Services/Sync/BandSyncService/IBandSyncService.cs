using AntennaScraper.Lib.Services.Data.BandService;

namespace AntennaScraper.Lib.Services.Sync.BandSyncService;

public interface IBandSyncService
{
    Task SyncBandsAsync(IEnumerable<BandDto> bands, CancellationToken cancellationToken = default);
}