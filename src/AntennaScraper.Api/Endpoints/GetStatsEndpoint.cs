using AntennaScraper.Api.Helper;
using AntennaScraper.Lib.Services.Stats;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AntennaScraper.Api.Endpoints;

public class GetStatsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder route)
    {
        route.MapGet("/stats", HandleAsync)
            .AddOpenApiOperationTransformer((o, _, _) =>
            {
                o.Summary = "Get counts of various entities in the database.";
                o.Description = "Returns statistics the number of antennas, bands, base stations, carriers, and providers stored in the database.";
                return Task.FromResult(o);
            });
    }

    private static async Task<Ok<StatsDto>> HandleAsync(IStatsService statsService, CancellationToken cancellationToken)
    {
        var stats = await statsService.GetStatsAsync(cancellationToken);
        return TypedResults.Ok(stats);
    }
}