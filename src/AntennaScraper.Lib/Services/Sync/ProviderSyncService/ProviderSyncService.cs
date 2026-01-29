using AntennaScraper.Lib.Entities;
using AntennaScraper.Lib.Services.Data.ProviderService;
using AntennaScraper.Lib.Services.Sync.BaseSyncService;
using AntennaScraper.Lib.Services.UnitOfWork;

namespace AntennaScraper.Lib.Services.Sync.ProviderSyncService;

internal class ProviderSyncService(IUnitOfWork uow, IBaseSyncService baseSync) : IProviderSyncService
{
    public async Task<SyncResult> SyncProvidersAsync(IEnumerable<ProviderDto> providers, CancellationToken cancellationToken)
    {
        var providersArr = providers as ProviderDto[] ?? providers.ToArray();
        return await uow.ExecuteTransactionAsync(async (token, context) =>
        {
            var newProviders = providersArr
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .DistinctBy(p => p.Id)
                .Select(p => new Provider
                {
                    ExternalId = p.Id,
                    Name = p.Name
                });
            var result = await baseSync.SyncObjectsAsync(newProviders, context.Providers, token,
                null,
                p => p.Name);
            await context.SaveChangesAsync(token);
            return result;
        }, cancellationToken);
    }
}