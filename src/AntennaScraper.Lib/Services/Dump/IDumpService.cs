namespace AntennaScraper.Lib.Services.Dump;

public interface IDumpService
{
    Task<Stream> DumpDbAsync(CancellationToken cancellationToken);
}