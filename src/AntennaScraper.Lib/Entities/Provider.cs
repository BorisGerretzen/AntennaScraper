namespace AntennaScraper.Lib.Entities;

public class Provider : ISyncEntity
{
    public string Name { get; set; } = null!;
    public ICollection<Carrier> Carriers { get; set; } = [];
    public ICollection<BaseStation> BaseStations { get; set; } = [];
    public int Id { get; set; }
    public uint RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long ExternalId { get; set; }
}