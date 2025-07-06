namespace AntennaScraper.Lib.Services.Data.CarrierService;

public record CarrierDto(int Id, int ProviderId, long FrequencyLow, long FrequencyHigh, int BandId);