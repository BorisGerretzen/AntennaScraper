using AntennaScraper.Api.Helper;
using AntennaScraper.Lib.Services.SyncLogService;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AntennaScraper.Api.Endpoints;

public class SyncLogEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder route)
    {
        route.MapGet("/log", HandleAsync)
            .AddOpenApiOperationTransformer((o, _, _) =>
            {
                o.Summary = "Get latest sync time";
                o.Description = "Gets the latest log entry for the sync process. Useful for automatically downloading new data only when there is an update.";
                return Task.FromResult(o);
            });
    }
    
    private static async Task<Ok<SyncLogDto?>> HandleAsync(ISyncLogService syncLogService, CancellationToken cancellationToken)
    {
        var log = await syncLogService.GetLastSyncLogAsync(cancellationToken);
        return TypedResults.Ok<SyncLogDto?>(log);
    }
}