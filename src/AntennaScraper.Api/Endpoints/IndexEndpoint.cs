using AntennaScraper.Api.Helper;

namespace AntennaScraper.Api.Endpoints;

public class IndexEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder route)
    {
        route.MapGet("/", Handle)
            .AddOpenApiOperationTransformer((o, _, _) =>
            {
                o.Summary = "Home page";
                o.Description = "Does nothing";
                return Task.FromResult(o);
            });
    }

    private const string Html = """
                                Welcome to the Antenna Scraper API!<br/>
                                Click <a href="/openapi.json">here</a> to view the available endpoints.<br/>
                                Click <a href="/dump">here</a> to download the latest sqlite dump file. Be patient! It might take a while...<br/>
                                """;
    
    private static HtmlResult Handle()
    {
        return new HtmlResult(Html);
    }
}