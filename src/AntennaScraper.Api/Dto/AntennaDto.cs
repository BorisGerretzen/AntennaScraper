using AntennaScraper.Lib.Entities;

namespace AntennaScraper.Api.Dto;

public record AntennaDto(
    decimal Frequency,
    decimal Height,
    decimal Direction,
    decimal TransmissionPower,
    string SatCode,
    bool IsDirectional,
    DateOnly? DateOfCommissioning,
    CarrierDto? Carrier
)
{
    public static AntennaDto? FromEntity(Antenna? antenna)
    {
        if (antenna == null) return null;
        return new AntennaDto(
            antenna.Frequency,
            antenna.Height,
            antenna.Direction,
            antenna.TransmissionPower,
            antenna.SatCode,
            antenna.IsDirectional,
            antenna.DateOfCommissioning,
            CarrierDto.FromEntity(antenna.Carrier)
        );
    }
}