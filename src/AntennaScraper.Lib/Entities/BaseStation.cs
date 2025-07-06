using NetTopologySuite.Geometries;

namespace AntennaScraper.Lib.Entities;

public class BaseStation : ISyncEntity
{
    public Point Location { get; set; } = null!;

    /// <summary>
    /// Do not use, only used for SQLite provider.
    /// </summary>
    public double Longitude { get; set; }
    
    /// <summary>
    /// Do not use, only used for SQLite provider.
    /// </summary>
    public double Latitude { get; set; }
    
    public string Municipality { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string City { get; set; } = null!;
    public bool IsSmallCell { get; set; }
    
    public int ProviderId { get; set; }
    public virtual Provider Provider { get; set; } = null!;
    
    public ICollection<Antenna> Antennas { get; set; } = [];
    
    public int Id { get; set; }
    public uint RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long ExternalId { get; set; }
}