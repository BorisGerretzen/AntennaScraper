using AntennaScraper.Lib.Services.Data.CarrierService;

namespace AntennaScraper.Lib.Services.Sync.CarrierSyncService;

public interface ICarrierSyncService
{
    Task SyncCarriersAsync(IEnumerable<CarrierDto> carriers, CancellationToken cancellationToken);
}