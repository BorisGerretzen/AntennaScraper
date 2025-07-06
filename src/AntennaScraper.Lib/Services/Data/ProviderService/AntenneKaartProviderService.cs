using System.Text.Json.Nodes;
using AntennaScraper.Lib.Services.External.AntenneKaartClient;

namespace AntennaScraper.Lib.Services.Data.ProviderService;

public class AntenneKaartProviderService(IAntenneKaartClient client) : IProviderService
{
    public async Task<List<ProviderDto>> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        var providers = await client.GetAllAsync<JsonObject>("providers", cancellationToken);
        return providers
            .Select(provider => new ProviderDto(
                provider["id"]!.GetValue<int>(),
                provider["name"]?.ToString() ?? "Unknown")
            )
            .Where(p => AntennaGlobals.AllowedExternalProviderIds.Contains(p.Id))
            .ToList();
    }
}