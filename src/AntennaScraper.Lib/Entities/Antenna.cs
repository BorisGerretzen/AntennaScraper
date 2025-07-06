namespace AntennaScraper.Lib.Entities;

public class Antenna : ISyncEntity
{
    public decimal Frequency { get; set; }
    public decimal Height { get; set; }
    public decimal Direction { get; set; }
    public decimal TransmissionPower { get; set; }

    public string SatCode { get; set; } = null!;
    public bool IsDirectional { get; set; }
    public DateOnly? DateOfCommissioning { get; set; }
    public DateOnly? DateLastChanged { get; set; }
    
    public int BaseStationId { get; set; }
    public virtual BaseStation BaseStation { get; set; } = null!;

    public int CarrierId { get; set; }
    public virtual Carrier Carrier { get; set; } = null!;

    public int Id { get; set; }
    public uint RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long ExternalId { get; set; }
}