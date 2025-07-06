using NetTopologySuite.Geometries;

namespace AntennaScraper.Lib.Services.External.AntenneRegisterClient.Dto;

public record AntenneRegisterBaseStation(long Id, List<long> AntennaIds, Point Location, string Municipality, string PostalCode, string City, bool IsSmallCell);