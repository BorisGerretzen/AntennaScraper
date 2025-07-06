using AntennaScraper.Lib.Entities;

namespace AntennaScraper.Api.Dto;

public record CarrierDto(
    long FrequencyLow,
    long FrequencyHigh,
    string? BandDescription
)
{
    public static CarrierDto? FromEntity(Carrier? carrier)
    {
        if (carrier == null) return null;
        return new CarrierDto(
            carrier.FrequencyLow,
            carrier.FrequencyHigh,
            carrier.Band.Description
        );
    }
}