namespace AntennaScraper.Api.Helper;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder Map<T>(this IEndpointRouteBuilder app) where T : IEndpoint
    {
        T.Map(app);
        return app;
    }
}