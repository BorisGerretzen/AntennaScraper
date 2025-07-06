using AntennaScraper.Lib.Data;

namespace AntennaScraper.Lib.Services.Data.BandService;

public class StaticBandService : IBandService
{
    public Task<List<BandDto>> GetBandsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(BandData.Bands.ToList());
    }
}