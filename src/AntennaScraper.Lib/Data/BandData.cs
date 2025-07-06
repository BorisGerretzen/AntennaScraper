using AntennaScraper.Lib.Services.Data.BandService;

namespace AntennaScraper.Lib.Data;

/// <summary>
/// Retrieved from https://antennekaart.nl/page/frequencies
/// </summary>
public static class BandData
{
    public static readonly IReadOnlyCollection<BandDto> Bands =
    [
        new(BandIds.Pamr, "450MHz PAMR", string.Empty),
        new(BandIds.Band28, "700MHz band 28", string.Empty),
        new(BandIds.Band20, "800MHz band 20", string.Empty),
        new(BandIds.Band8, "900MHz band 8", string.Empty),
        new(BandIds.Band32, "1400MHz band 32", string.Empty),
        new(BandIds.Band3, "1800MHz band 3", string.Empty),
        new(BandIds.Band1, "2100MHz band 1", string.Empty),
        new(BandIds.Band38, "2600MHz unpaired band 38", string.Empty),
        new(BandIds.Band7, "2600MHz paired band 7", string.Empty),
        new(BandIds.BandN78, "3500MHz band n78", string.Empty)
    ];
}