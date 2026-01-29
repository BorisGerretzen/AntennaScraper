using AntennaScraper.Api.Helper;
using AntennaScraper.Lib.Services.Dump;

namespace AntennaScraper.Api.Endpoints;

public class DumpEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder route)
    {
        route.MapGet("/dump", async (CancellationToken cancellationToken, IDumpService dumpService) =>
            {
                var stream = await dumpService.DumpDbAsync(cancellationToken);
                return TypedResults.File(stream, "application/x-sqlite3", "dump.sqlite");
            })
            .AddOpenApiOperationTransformer((o, _, _) =>
            {
                o.Summary = "Dump the entire database as a SQLite file (might take a while to download).";
                o.Description = "Dumps the entire database to SQLite and returns it as a file download. " +
                                "Note that this might take a while to generate and download, " +
                                "for me at the time of writing this endpoint takes about a minute before I have the dump on my drive.";
                return Task.FromResult(o);
            });
    }
}