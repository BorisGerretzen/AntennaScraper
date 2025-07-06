namespace AntennaScraper.Lib.Entities;

public class Carrier : ISyncEntity
{
    public long FrequencyLow { get; set; }
    public long FrequencyHigh { get; set; }

    public int ProviderId { get; set; }
    public virtual Provider Provider { get; set; } = null!;

    public int BandId { get; set; }
    public virtual Band Band { get; set; } = null!;
    public int Id { get; set; }
    public uint RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public long ExternalId { get; set; }
}