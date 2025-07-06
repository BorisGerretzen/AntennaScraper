using AntennaScraper.Lib.Entities;
using AntennaScraper.Lib.Helpers;
using AntennaScraper.Lib.Services.External.AntenneRegisterClient.Dto;
using AntennaScraper.Lib.Services.Sync.BaseSyncService;
using AntennaScraper.Lib.Services.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntennaScraper.Lib.Services.Sync.BaseStationSyncService;

internal class BaseStationSyncService(IUnitOfWork uow, IBaseSyncService baseSync, ILogger<BaseStationSyncService> logger) : IBaseStationSyncService
{
    private List<CarrierMatch>? _carriers;

    public async Task SyncBaseStationsAsync(IEnumerable<AntenneRegisterBaseStation> baseStations, Dictionary<long, List<AntenneRegisterAntenna>> antennasByBaseStationId,
        CancellationToken cancellationToken)
    {
        var baseStationList = baseStations as List<AntenneRegisterBaseStation> ?? baseStations.ToList();

        await uow.ExecuteTransactionAsync(async (token, context) =>
        {
            var incomingBaseStations = new List<BaseStation>();
            var incomingAntennas = new List<Antenna>();
            
            foreach (var bs in baseStationList)
            {
                if (!antennasByBaseStationId.TryGetValue(bs.Id, out var antennaDtos) || antennaDtos.Count == 0)
                {
                    logger.LogWarning("Base station {BaseStationId} has no antennas, skipping.", bs.Id);
                    continue;
                }

                var providerId = await MatchProviderIdAsync(antennaDtos, context.Carriers, token);
                if (providerId == null)
                {
                    logger.LogWarning("Base station {BaseStationId} has no antennas with a matching provider, skipping.", bs.Id);
                    continue;
                }

                var baseStation = new BaseStation
                {
                    ExternalId = bs.Id,
                    Location = bs.Location,
                    ProviderId = providerId.Value,
                    City = bs.City,
                    Municipality = bs.Municipality,
                    IsSmallCell = bs.IsSmallCell,
                    PostalCode = bs.PostalCode
                };

                if (_carriers == null) throw new InvalidOperationException("Carriers not loaded, cannot find closest carrier.");

                var antennas = new List<Antenna>();
                foreach (var antenna in antennaDtos)
                {
                    var frequency = FrequencyHelpers.ParseFrequency(antenna.Frequency);
                    var carrier = GetClosestCarrier(frequency, _carriers);
                    if (carrier == null)
                    {
                        logger.LogWarning("No matching carrier found for antenna {AntennaId} with frequency {Frequency}, skipping.", antenna.Id, frequency);
                        continue;
                    }

                    antennas.Add(new Antenna
                    {
                        ExternalId = antenna.Id,
                        Frequency = frequency,
                        CarrierId = carrier.Id,
                        TransmissionPower = antenna.TransmissionPower,
                        Direction = antenna.Direction,
                        Height = antenna.Height,
                        IsDirectional = antenna.IsDirectional,
                        SatCode = antenna.SatCode,
                        DateOfCommissioning = antenna.DateOfCommissioning,
                        DateLastChanged = antenna.DateLastChanged,
                        BaseStation = baseStation,
                    });
                }

                incomingAntennas.AddRange(antennas);
                incomingBaseStations.Add(baseStation);
            }

            await baseSync.SyncObjectsAsync(incomingBaseStations, context.BaseStations, token, 
                additionalDeleteCondition: bs => true,
                bs => bs.Location,
                bs => bs.ProviderId,
                bs => bs.City,
                bs => bs.Municipality,
                bs => bs.IsSmallCell,
                bs => bs.PostalCode);
            await context.SaveChangesAsync(token);

            foreach (var antenna in incomingAntennas)
            {
                antenna.BaseStationId = antenna.BaseStation.Id;
                antenna.BaseStation = null!;
            }

            await baseSync.SyncObjectsAsync(incomingAntennas, context.Antennas, token,
                additionalDeleteCondition: antenna => true,
                a => a.Frequency,
                a => a.CarrierId,
                a => a.TransmissionPower,
                a => a.Direction,
                a => a.Height,
                a => a.IsDirectional,
                a => a.SatCode,
                a => a.BaseStationId,
                a => a.DateOfCommissioning,
                a => a.DateLastChanged);
            await context.SaveChangesAsync(token);
        }, cancellationToken);
    }

    private async Task<int?> MatchProviderIdAsync(
        IEnumerable<AntenneRegisterAntenna> antennas,
        DbSet<Carrier> carriers,
        CancellationToken cancellationToken)
    {
        antennas = antennas as IReadOnlyCollection<AntenneRegisterAntenna> ?? antennas.ToList();
        _carriers ??= await carriers.AsNoTracking()
            .Include(c => c.Provider)
            .Select(c => CarrierMatch.FromCarrier(c))
            .ToListAsync(cancellationToken);

        if (_carriers.Count == 0) throw new InvalidOperationException("No carriers found in the database, run that import first.");

        var frequencies = antennas
            .Select(a => FrequencyHelpers.ParseFrequency(a.Frequency))
            .ToList();

        var providers = frequencies
            .Select(f => new
            {
                Frequency = f,
                Carrier = GetClosestCarrier(f, _carriers)
            })
            .Select(f => new
            {
                f.Frequency,
                ProviderInternal = f.Carrier?.ProviderInternalId,
                ProviderExternal = f.Carrier?.ProviderExternalId
            })
            .ToList();

        var uniqueProviders = providers.Select(p => p.ProviderExternal).Where(p => p.HasValue).Distinct().ToList();

        switch (uniqueProviders.Count)
        {
            case 2 when uniqueProviders.Contains(AntennaGlobals.KpnProviderId) && uniqueProviders.Contains(AntennaGlobals.VodafoneProviderId) &&
                        providers.Where(p => p.Frequency is > 758_000_000 and < 778_000_000).Select(p => p.ProviderExternal).Distinct().Count() == 2:
                logger.LogWarning("Tampnet detected, skipping");
                return null;
            case > 1:
                logger.LogWarning("Multiple providers found for antennas: {@Providers}. Cannot determine a single provider.", providers);
                return null;
            case 0:
                logger.LogWarning("No matching provider found for antennas with frequencies: {@Frequencies}.", frequencies.Distinct().ToList());
                return null;
            default:
                return providers.First(p => p.ProviderInternal.HasValue).ProviderInternal;
        }
    }

    private CarrierMatch? GetClosestCarrier(long frequency, ICollection<CarrierMatch> carriers)
    {
        var closestCarrier = carriers.FirstOrDefault(c => c.ContainsFrequency(frequency));

        // Log warning if frequency is on border of two carriers spectrum
        var isEdgeCase = carriers.Any(c => c.IsEdgeCase(frequency));
        if (isEdgeCase) logger.LogWarning("Frequency {Frequency} is an edge case.", frequency);

        return closestCarrier;
    }

    private record CarrierMatch(int Id, long FrequencyMin, long FrequencyMax, long ProviderExternalId, int ProviderInternalId)
    {
        public bool ContainsFrequency(long frequency)
        {
            return frequency >= FrequencyMin && frequency <= FrequencyMax;
        }

        public bool IsEdgeCase(long frequency)
        {
            return frequency == FrequencyMin || frequency == FrequencyMax;
        }

        public static CarrierMatch FromCarrier(Carrier carrier)
        {
            return new CarrierMatch(carrier.Id, carrier.FrequencyLow, carrier.FrequencyHigh, carrier.Provider.ExternalId, carrier.ProviderId);
        }
    }
}