using AntennaScraper.Lib.Services.External.AntenneRegisterClient.Dto;

namespace AntennaScraper.Lib.Services.Sync.BaseStationSyncService;

public interface IBaseStationSyncService
{
    Task SyncBaseStationsAsync(IEnumerable<AntenneRegisterBaseStation> baseStations, Dictionary<long, List<AntenneRegisterAntenna>> antennasByBaseStationId,
        CancellationToken cancellationToken);
}