namespace AntennaScraper.Lib.Services.Data.CarrierService;

public interface ICarrierService
{
    Task<List<CarrierDto>> GetCarriersAsync(CancellationToken cancellationToken = default);
}