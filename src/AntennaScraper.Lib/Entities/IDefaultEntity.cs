namespace AntennaScraper.Lib.Entities;

public interface IDefaultEntity
{
    int Id { get; set; }
    uint RowVersion { get; set; }
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; set; }
}