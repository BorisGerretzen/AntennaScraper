using AntennaScraper.Lib.Services.External.AntenneRegisterClient.Dto;

namespace AntennaScraper.Lib.Services.External.AntenneRegisterClient;

public interface IAntenneRegisterClient
{
    /// <summary>
    /// Gets a list of all base stations from AntenneRegister.
    /// </summary>
    Task<List<AntenneRegisterBaseStation>> GetBaseStationsAsync(CancellationToken cancellation);

    /// <summary>
    /// Gets antenna information for antennas grouped by base station ID.
    /// </summary>
    /// <param name="baseStationIds">{BS: [AntennaId]}</param>
    /// <param name="cancellation">Cancellation token</param>
    /// <returns>{BS: [AntennaData]}</returns>
    Task<Dictionary<long, List<AntenneRegisterAntenna>>> GetAntennasByBaseStationIdAsync(Dictionary<long, List<long>> baseStationIds, CancellationToken cancellation);
}