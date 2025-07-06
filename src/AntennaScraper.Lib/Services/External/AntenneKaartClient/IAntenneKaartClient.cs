namespace AntennaScraper.Lib.Services.External.AntenneKaartClient;

public interface IAntenneKaartClient
{
    Task<List<T>> GetAllAsync<T>(string url, CancellationToken cancellationToken = default) where T : class;
}