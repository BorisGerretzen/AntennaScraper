using System.Text.Json;
using NetTopologySuite.IO.Converters;

namespace AntennaScraper.Lib;

public class AntennaGlobals
{
    public const string DbConnectionStringName = "AntennaDb";

    public const int KpnProviderId = 1;
    public const int OdidoProviderId = 3;
    public const int VodafoneProviderId = 4;

    public static readonly IReadOnlySet<int> AllowedExternalProviderIds = new HashSet<int>
    {
        KpnProviderId,
        OdidoProviderId,
        VodafoneProviderId
    };

    public static readonly JsonSerializerOptions GeoSerializer = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new GeoJsonConverterFactory()
        }
    };
}