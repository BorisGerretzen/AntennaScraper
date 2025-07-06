using AntennaScraper.Lib.Data;

namespace AntennaScraper.Lib.Services.Data.CarrierService;

public class  StaticCarrierService : ICarrierService
{
    public Task<List<CarrierDto>> GetCarriersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CarrierData.Carriers.ToList());
    }
}