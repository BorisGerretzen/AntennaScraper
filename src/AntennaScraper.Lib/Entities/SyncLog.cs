namespace AntennaScraper.Lib.Entities;

public class SyncLog : IDefaultEntity
{
    public DateTime SyncStartedAt { get; set; }
    public DateTime SyncEndedAt { get; set; }
    public bool IsSuccessful { get; set; }
    
    public int Id { get; set; }
    public uint RowVersion { get; set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; set; }
}