namespace AntennaScraper.Lib.Entities;

public class Band : ISyncEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Id { get; set; }
    public uint RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long ExternalId { get; set; }
}