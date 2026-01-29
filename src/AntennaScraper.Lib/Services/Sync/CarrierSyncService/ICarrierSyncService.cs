using AntennaScraper.Lib.Services.Data.CarrierService;
using AntennaScraper.Lib.Services.Sync.BaseSyncService;

namespace AntennaScraper.Lib.Services.Sync.CarrierSyncService;

public interface ICarrierSyncService
{
    Task<SyncResult> SyncCarriersAsync(IEnumerable<CarrierDto> carriers, CancellationToken cancellationToken);
}