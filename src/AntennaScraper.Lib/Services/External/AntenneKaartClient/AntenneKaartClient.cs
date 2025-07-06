using System.Net.Http.Json;

namespace AntennaScraper.Lib.Services.External.AntenneKaartClient;

public class AntenneKaartClient(HttpClient client) : IAntenneKaartClient
{
    public async Task<AntenneKaartResponse<T>> GetAsync<T>(string url, CancellationToken cancellationToken = default) where T : class
    {
        var response = await client.GetAsync(url, cancellationToken);
        var message = response.EnsureSuccessStatusCode();

        var deserialized = await message.Content.ReadFromJsonAsync<AntenneKaartResponse<T>>(cancellationToken);
        if (deserialized is null) throw new InvalidOperationException("Failed to deserialize response.");

        return deserialized;
    }

    public async Task<List<T>> GetAllAsync<T>(string url, CancellationToken cancellationToken = default) where T : class
    {
        var results = new List<T>();
        var nextUrl = url;

        while (!string.IsNullOrEmpty(nextUrl))
        {
            var response = await GetAsync<T>(nextUrl, cancellationToken);
            results.AddRange(response.Results);
            nextUrl = response.Next;
        }

        return results;
    }
}