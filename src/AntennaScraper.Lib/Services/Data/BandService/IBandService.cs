namespace AntennaScraper.Lib.Services.Data.BandService;

public interface IBandService
{
    Task<List<BandDto>> GetBandsAsync(CancellationToken cancellationToken);
}