using AntennaScraper.Lib.Services.External.AntenneRegisterClient.Dto;
using AntennaScraper.Lib.Services.Sync.BaseSyncService;

namespace AntennaScraper.Lib.Services.Sync.BaseStationSyncService;

public interface IBaseStationSyncService
{
    Task<(SyncResult Basestations, SyncResult Antennas)> SyncBaseStationsAsync(IEnumerable<AntenneRegisterBaseStation> baseStations, Dictionary<long, List<AntenneRegisterAntenna>> antennasByBaseStationId,
        CancellationToken cancellationToken);
}