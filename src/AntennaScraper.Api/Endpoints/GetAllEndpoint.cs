using AntennaScraper.Api.Dto;
using AntennaScraper.Api.Helper;
using AntennaScraper.Lib.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AntennaScraper.Api.Endpoints;

public class GetAllEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder route)
    {
        route.MapGet("/all", HandleAsync)
            .AddOpenApiOperationTransformer((o, _, _) =>
            {
                o.Description = "Gets all base stations with their antennas, carriers, bands, and providers. " +
                                "I don't recommend using this endpoint unless you really dislike SQLite as this will return a lot of data. " +
                                "The dump endpoint is a better alternative for most use cases and returns a SQLite dump of the same data.";
                o.Summary = "Gets all base stations with their antennas, carriers, bands, and providers.";
                return Task.FromResult(o);
            });
    }

    private static async Task<Ok<List<BaseStationDto>>> HandleAsync(IDbContextFactory<AntennaDbContext> contextFactory, CancellationToken cancellationToken)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var results = await dbContext.BaseStations
            .AsNoTracking()
            .Include(bs => bs.Antennas).ThenInclude(a => a.Carrier).ThenInclude(c => c.Band)
            .Include(bs => bs.Provider)
            .AsSplitQuery()
            .Select(bs => BaseStationDto.FromEntity(bs))
            .ToListAsync(cancellationToken);
        return TypedResults.Ok(results);
    }
}