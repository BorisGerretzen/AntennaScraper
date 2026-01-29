using AntennaScraper.Lib.Entities;
using AntennaScraper.Lib.Services.Data.BandService;
using AntennaScraper.Lib.Services.Sync.BaseSyncService;
using AntennaScraper.Lib.Services.UnitOfWork;

namespace AntennaScraper.Lib.Services.Sync.BandSyncService;

internal class BandSyncService(IUnitOfWork uow, IBaseSyncService baseSync) : IBandSyncService
{
    public async Task<SyncResult> SyncBandsAsync(IEnumerable<BandDto> bands, CancellationToken cancellationToken = default)
    {
        var bandsArr = bands as BandDto[] ?? bands.ToArray();
        return await uow.ExecuteTransactionAsync(async (token, context) =>
        {
            var newBands = bandsArr
                .Where(b => !string.IsNullOrWhiteSpace(b.Alias))
                .DistinctBy(b => b.Id)
                .Select(b => new Band
                {
                    ExternalId = b.Id,
                    Name = b.Alias,
                    Description = b.Description
                });

            var result = await baseSync.SyncObjectsAsync(newBands, context.Bands, token,
                null,
                b => b.Name,
                b => b.Description
            );
            await context.SaveChangesAsync(token);
            return result;
        }, cancellationToken);
    }
}