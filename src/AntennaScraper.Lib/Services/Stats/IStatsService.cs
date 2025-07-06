namespace AntennaScraper.Lib.Services.Stats;

public interface IStatsService
{
    Task<StatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}