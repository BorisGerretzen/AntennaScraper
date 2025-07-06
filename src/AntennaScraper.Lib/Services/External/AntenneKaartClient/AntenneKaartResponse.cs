namespace AntennaScraper.Lib.Services.External.AntenneKaartClient;

public sealed class AntenneKaartResponse<T> where T : class
{
    public int Count { get; set; }
    public string? Next { get; set; }
    public string? Previous { get; set; }
    public List<T> Results { get; set; } = new();
}