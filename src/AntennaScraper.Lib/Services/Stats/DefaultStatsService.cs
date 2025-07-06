using AntennaScraper.Lib.Services.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace AntennaScraper.Lib.Services.Stats;

public class DefaultStatsService(IUnitOfWork uow) : IStatsService
{
    public async Task<StatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var numAntennas = await uow.ExecuteAsync((ct, ctx) => ctx.Antennas.CountAsync(ct), cancellationToken);
        var numBands = await uow.ExecuteAsync((ct, ctx) => ctx.Bands.CountAsync(ct), cancellationToken);
        var numBaseStations = await uow.ExecuteAsync((ct, ctx) => ctx.BaseStations.CountAsync(ct), cancellationToken);
        var numCarriers = await uow.ExecuteAsync((ct, ctx) => ctx.Carriers.CountAsync(ct), cancellationToken);
        var numProviders = await uow.ExecuteAsync((ct, ctx) => ctx.Providers.CountAsync(ct), cancellationToken);
        
        return new StatsDto(numAntennas, numBands, numBaseStations, numCarriers, numProviders);
    }
}