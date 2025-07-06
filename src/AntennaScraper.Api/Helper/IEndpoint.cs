namespace AntennaScraper.Api.Helper;

public interface IEndpoint
{
    static abstract void Map(IEndpointRouteBuilder route);
}