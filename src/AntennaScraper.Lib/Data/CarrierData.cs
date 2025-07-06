using AntennaScraper.Lib.Services.Data.CarrierService;

namespace AntennaScraper.Lib.Data;

/// <summary>
/// Retrieved from https://antennekaart.nl/page/frequencies
/// </summary>
public static class CarrierData
{
    private const int Kpn = AntennaGlobals.KpnProviderId;
    private const int Odido = AntennaGlobals.OdidoProviderId;
    private const int Vodafone = AntennaGlobals.VodafoneProviderId;

    public static readonly IReadOnlyCollection<CarrierDto> Carriers = new CarrierDto[]
        {
            // 700 MHz band 28
            new(694200, Vodafone, 758, 768, BandIds.Band28),
            new(694201, Kpn, 768, 778, BandIds.Band28),
            new(694202, Odido, 778, 788, BandIds.Band28),

            // 800 MHz band 20
            new(694203, Odido, 791, 801, BandIds.Band20),
            new(694204, Vodafone, 801, 811, BandIds.Band20),
            new(694205, Kpn, 811, 821, BandIds.Band20),

            // 900 MHz band 8
            new(694206, Vodafone, 925, 935, BandIds.Band8),
            new(694207, Kpn, 935, 945, BandIds.Band8),
            new(694208, Odido, 945, 960, BandIds.Band8),

            // 1400 MHz band 32
            new(694209, Vodafone, 1452, 1467, BandIds.Band32),
            new(694210, Kpn, 1467, 1482, BandIds.Band32),
            new(694211, Odido, 1482, 1492, BandIds.Band32),

            // 1800 MHz band 3
            new(694212, Kpn, 1805, 1825, BandIds.Band3),
            new(694213, Vodafone, 1825, 1845, BandIds.Band3),
            new(694214, Odido, 1845, 1875, BandIds.Band3),

            // 2100 MHz band 1
            new(694215, Vodafone, 2110, 2130, BandIds.Band1),
            new(694216, Odido, 2130, 2150, BandIds.Band1),
            new(694217, Kpn, 2150, 2170, BandIds.Band1),

            // 2600 MHz band 38
            new(694218, Odido, 2565, 2590, BandIds.Band38),
            new(694219, Kpn, 2590, 2620, BandIds.Band38),

            // 2600 MHz band 7
            new(694220, Vodafone, 2620, 2650, BandIds.Band7),
            new(694221, Odido, 2650, 2655, BandIds.Band7),
            new(694222, Kpn, 2655, 2665, BandIds.Band7),
            new(694223, Odido, 2665, 2685, BandIds.Band7),

            // 3500 MHz band n78
            new(694224, Vodafone, 3450, 3550, BandIds.BandN78),
            new(694225, Odido, 3550, 3650, BandIds.BandN78),
            new(694226, Kpn, 3650, 3750, BandIds.BandN78)
        }
        .Select(c => c with { FrequencyHigh = c.FrequencyHigh * 1_000_000, FrequencyLow = c.FrequencyLow * 1_000_000 })
        .ToList();
}