namespace AntennaScraper.Lib.Services.Data.ProviderService;

public interface IProviderService
{
    Task<List<ProviderDto>> GetProvidersAsync(CancellationToken cancellationToken = default);
}