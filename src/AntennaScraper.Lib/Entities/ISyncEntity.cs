namespace AntennaScraper.Lib.Entities;

public interface ISyncEntity : IDefaultEntity
{
    long ExternalId { get; set; }
}