using AntennaScraper.Lib.Entities;

namespace AntennaScraper.Api.Dto;

public record ProviderDto(int Id, string Name)
{
    public static ProviderDto? FromEntity(Provider? model)
    {
        if (model == null) return null;
        return new ProviderDto(model.Id, model.Name);
    }
}