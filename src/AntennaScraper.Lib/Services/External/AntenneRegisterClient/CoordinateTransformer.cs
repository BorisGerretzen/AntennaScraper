using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace AntennaScraper.Lib.Services.External.AntenneRegisterClient;

public class CoordinateTransformer
{
    /// <summary>
    /// https://epsg.io/28992
    /// </summary>
    private const string RdNewWkt =
        "PROJCS[\"Amersfoort / RD New\",GEOGCS[\"Amersfoort\",DATUM[\"Amersfoort\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128],TOWGS84[565.4171,50.3319,465.5524,1.9342,-1.6677,9.1019,4.0725]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4289\"]],PROJECTION[\"Oblique_Stereographic\"],PARAMETER[\"latitude_of_origin\",52.1561605555556],PARAMETER[\"central_meridian\",5.38763888888889],PARAMETER[\"scale_factor\",0.9999079],PARAMETER[\"false_easting\",155000],PARAMETER[\"false_northing\",463000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"28992\"]]";
    
    /// <summary>
    /// Converts RD New coordinates to WGS84.
    /// </summary>
    /// <param name="points">Points to convert.</param>
    /// <returns>Transformed points in WGS84.</returns>
    public static List<Point> TransformToWgs84(List<Point> points)
    {
        var csFact = new CoordinateSystemFactory();
        var ctFact = new CoordinateTransformationFactory();
        
        var cs28992 = csFact.CreateFromWkt(RdNewWkt);
        var cs4326 = GeographicCoordinateSystem.WGS84;
        
        var transform = ctFact.CreateFromCoordinateSystems(cs28992, cs4326);
        var transformed = transform.MathTransform.TransformList(
            points.Select(p => new[] { p.X, p.Y }).ToList());
        
        var result = transformed
            .Select(coord => new Point(coord[0], coord[1]) { SRID = 4326 })
            .ToList();
        return result;
    }
}