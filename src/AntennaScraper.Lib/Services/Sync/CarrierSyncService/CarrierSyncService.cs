using AntennaScraper.Lib.Entities;
using AntennaScraper.Lib.Services.Data.CarrierService;
using AntennaScraper.Lib.Services.Sync.BaseSyncService;
using AntennaScraper.Lib.Services.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace AntennaScraper.Lib.Services.Sync.CarrierSyncService;

internal class CarrierSyncService(IUnitOfWork uow, IBaseSyncService baseSync) : ICarrierSyncService
{
    public async Task SyncCarriersAsync(IEnumerable<CarrierDto> carriers, CancellationToken cancellationToken)
    {
        var carriersArr = carriers as List<CarrierDto> ?? carriers.ToList();

        await uow.ExecuteTransactionAsync(async (token, context) =>
        {
            var wantedProviders = carriersArr.Select(c => (long)c.ProviderId).Distinct().ToHashSet();
            var wantedBands = carriersArr.Select(c => (long)c.BandId).Distinct().ToHashSet();

            var providersMap = await context.Providers
                .AsNoTracking()
                .Where(p => wantedProviders.Contains(p.ExternalId))
                .Select(p => new
                {
                    p.ExternalId,
                    p.Id
                })
                .ToDictionaryAsync(p => p.ExternalId, p => p.Id, token);

            var bandsMap = await context.Bands
                .AsNoTracking()
                .Where(b => wantedBands.Contains(b.ExternalId))
                .Select(b => new
                {
                    b.ExternalId,
                    b.Id
                })
                .ToDictionaryAsync(b => b.ExternalId, b => b.Id, token);

            var newCarriers = carriersArr
                .DistinctBy(c => c.Id)
                .Select(c => new Carrier
                {
                    ExternalId = c.Id,
                    FrequencyLow = c.FrequencyLow,
                    FrequencyHigh = c.FrequencyHigh,
                    ProviderId = providersMap.GetValueOrDefault(c.ProviderId, -1),
                    BandId = bandsMap.GetValueOrDefault(c.BandId, -1)
                })
                .ToList();

            if (newCarriers.Any(c => c.ProviderId == -1 || c.BandId == -1)) throw new InvalidOperationException("Some carriers have invalid provider or band IDs.");

            await baseSync.SyncObjectsAsync(newCarriers, context.Carriers, token,
                null,
                c => c.FrequencyLow,
                c => c.FrequencyHigh,
                c => c.ProviderId,
                c => c.BandId);
            await context.SaveChangesAsync(token);
        }, cancellationToken);
    }
}