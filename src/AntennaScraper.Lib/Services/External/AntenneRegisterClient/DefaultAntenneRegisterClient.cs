using System.Text.Json;
using AntennaScraper.Lib.Services.External.AntenneRegisterClient.Dto;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace AntennaScraper.Lib.Services.External.AntenneRegisterClient;

public class DefaultAntenneRegisterClient(HttpClient client) : IAntenneRegisterClient
{
    private const int PageSize = 50_000;

    private const string BaseStationFilter = """
                                             <fes:Filter xmlns:fes="http://www.opengis.net/fes/2.0">
                                               <fes:PropertyIsEqualTo>
                                                 <fes:ValueReference>MOBIELE_COMMUNICATIE</fes:ValueReference>
                                                 <fes:Literal>1</fes:Literal>
                                               </fes:PropertyIsEqualTo>
                                             </fes:Filter>
                                             """;
    
    public async Task<List<AntenneRegisterBaseStation>> GetBaseStationsAsync(CancellationToken cancellation)
    {
        var baseStations = new List<AntenneRegisterBaseStation>();

        var queryParams = new Dictionary<string, string>
        {
            ["outputformat"] = "application/json",
            ["service"] = "WFS",
            ["version"] = "2.0.0",
            ["request"] = "GetFeature",
            ["typeName"] = "Antennes",
            ["startIndex"] = "0",
            ["sortBy"] = "ID",
            ["count"] = PageSize.ToString(),
            ["filter"] = BaseStationFilter,
        };
        
        var startIndex = 0;
        var reachedEnd = false;

        while (!reachedEnd)
        {
            var url = "https://antenneregister.nl/mapserver/wfs?" +
                      string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var message = await client.GetAsync(url, cancellation);
            message.EnsureSuccessStatusCode();

            using var ms = new MemoryStream();
            await using (await message.Content.ReadAsStreamAsync(cancellation))
            {
                await message.Content.CopyToAsync(ms, cancellation);
            }

            ms.Seek(0, SeekOrigin.Begin);
            var featureCollection = await JsonSerializer.DeserializeAsync<FeatureCollection>(ms, AntennaGlobals.GeoSerializer, cancellation);
            if (featureCollection == null || featureCollection.Count == 0) throw new InvalidOperationException("Invalid response from Antenne Register API.");

            foreach (var feature in featureCollection)
            {
                var id = (long)(decimal) feature.Attributes["ID"];
                var point = feature.Geometry as Point;
                if (point == null) continue;

                var antennaIdsString = (string)feature.Attributes["ANT_IDS"];
                var antennaIds = antennaIdsString.Split(",", StringSplitOptions.TrimEntries)
                    .Select(long.Parse);
                
                var municipality = (string)feature.Attributes["GEMEENTE"];
                var postalCode = (string)feature.Attributes["POSTCODE"];
                var city = (string)feature.Attributes["WOONPLAATSNAAM"];

                baseStations.Add(new AntenneRegisterBaseStation(id, antennaIds.ToList(), point, municipality, postalCode, city, false));
            }
            
            var coordinates = featureCollection
                .Select(f => f.Geometry as Point)
                .Where(p => p != null)
                .ToList();
            var transformedCoordinates = CoordinateTransformer.TransformToWgs84(coordinates!);
            for (var i = 0; i < transformedCoordinates.Count; i++)
            {
                var transformedPoint = transformedCoordinates[i];
                var bsIndex = baseStations.Count - featureCollection.Count + i;
                var originalBs = baseStations[bsIndex];
                var updatedBs = originalBs with { Location = transformedPoint };
                baseStations[bsIndex] = updatedBs;
            }
            
            startIndex += featureCollection.Count;
            queryParams["startIndex"] = startIndex.ToString();
            reachedEnd = featureCollection.Count < PageSize;
        }

        return baseStations;
    }

    public async Task<Dictionary<long, List<AntenneRegisterAntenna>>> GetAntennasByBaseStationIdAsync(Dictionary<long, List<long>> baseStationIds, CancellationToken cancellation)
    {
        var reverseMap = baseStationIds
            .SelectMany(kvp => kvp.Value.Select(id => new { BaseStationId = kvp.Key, AntennaId = id }))
            .ToDictionary(x => x.AntennaId, x => x.BaseStationId);
        
        var returnMap = new Dictionary<long, List<AntenneRegisterAntenna>>();

        var parameters = new Dictionary<string, string>
        {
            ["outputformat"] = "application/json",
            ["service"] = "WFS",
            ["version"] = "2.0.0",
            ["request"] = "GetFeature",
            ["typeName"] = "Antennes_Groepen",
            ["sortBy"] = "ID",
            ["count"] = PageSize.ToString(),
            ["startIndex"] = "0",
        };
        
        var startIndex = 0;
        var reachedEnd = false;

        while (!reachedEnd)
        {
            var url = "https://antenneregister.nl/mapserver/wfs?" +
                      string.Join("&", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            var message = await client.GetAsync(url, cancellation);
            message.EnsureSuccessStatusCode();

            using var ms = new MemoryStream();
            await using (await message.Content.ReadAsStreamAsync(cancellation))
            {
                await message.Content.CopyToAsync(ms, cancellation);
            }

            ms.Seek(0, SeekOrigin.Begin);
            var featureCollection = await JsonSerializer.DeserializeAsync<FeatureCollection>(ms, AntennaGlobals.GeoSerializer, cancellation);
            if (featureCollection == null || featureCollection.Count == 0) throw new InvalidOperationException("Invalid response from Antenne Register API.");

            foreach (var feature in featureCollection)
            {
                var id = (long)(decimal)feature.Attributes["ID"];
                var aiId = (long)(decimal)feature.Attributes["AI_ID"];
                if(!reverseMap.TryGetValue(aiId, out var baseStationId)) continue;
                var satCode = (string)feature.Attributes["SAT_CODE"];
                var isDirectional = feature.Attributes["DIR_NONDIR"]?.ToString() == "D";
                var height = (decimal)feature.Attributes["HOOGTE"];
                var direction = (decimal?)feature.Attributes.GetOptionalValue("HOOFDSTRAALRICHTING") ?? 0.0m;
                var transmissionPower = (decimal)feature.Attributes["ZENDVERMOGEN"];
                var frequency = (string)feature.Attributes["FREQUENTIE"];
            
                var dateOfCommissioningString = (string)feature.Attributes["DATUM_INGEBRUIKNAME"];
                DateOnly? dateOfCommissioning = null;
                if (!string.IsNullOrEmpty(dateOfCommissioningString) && DateOnly.TryParse(dateOfCommissioningString, out var parsedDate))
                {
                    dateOfCommissioning = parsedDate;
                }
            
                var dateLastChangedString = (string)feature.Attributes["DATUM_WIJZIGING"];
                DateOnly? dateLastChanged = null;
                if (!string.IsNullOrEmpty(dateLastChangedString) && DateOnly.TryParse(dateLastChangedString, out var parsedDateLastChanged))
                {
                    dateLastChanged = parsedDateLastChanged;
                }

                var antenna = new AntenneRegisterAntenna(id, satCode, isDirectional, height, direction, transmissionPower, frequency, dateOfCommissioning, dateLastChanged);
                if (!returnMap.TryGetValue(baseStationId, out var antennas))
                {
                    antennas = [];
                    returnMap[baseStationId] = antennas;
                }
                antennas.Add(antenna);
            }
            
            startIndex += featureCollection.Count;
            parameters["startIndex"] = startIndex.ToString();
            reachedEnd = featureCollection.Count < PageSize;
        }

        return returnMap;
    }
}