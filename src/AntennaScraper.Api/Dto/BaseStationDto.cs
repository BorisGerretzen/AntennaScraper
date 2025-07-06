using AntennaScraper.Lib.Entities;
using NetTopologySuite.Geometries;

namespace AntennaScraper.Api.Dto;

public record BaseStationDto(
    Point Location,
    string ProviderName,
    string Municipality,
    string PostalCode,
    string City,
    bool IsSmallCell,
    List<AntennaDto>? Antennas
)
{
    public static BaseStationDto FromEntity(BaseStation baseStation)
    {
        return new BaseStationDto(
            baseStation.Location,
            baseStation.Provider.Name,
            baseStation.Municipality,
            baseStation.PostalCode,
            baseStation.City,
            baseStation.IsSmallCell,
            baseStation.Antennas.Select(AntennaDto.FromEntity).Where(a => a != null).ToList()!
        );
    }
}