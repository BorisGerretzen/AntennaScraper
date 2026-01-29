using AntennaScraper.Lib;
using AntennaScraper.Lib.Services.External.AntenneRegisterClient.Dto;
using AntennaScraper.Lib.Services.Sync.BaseStationSyncService;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;

namespace AntennaScraper.Tests.Lib.Tests.Services.Sync;

[Collection("PostGIS")]
public class BaseStationSyncServicePostgisTests(PostgisFixture fixture) : IAsyncLifetime
{
    private AntennaDbContext _context = null!;
    private BaseStationSyncService _baseStationSyncService = null!;
    private Provider _kpnProvider = null!;
    private Provider _vodafoneProvider = null!;
    private Provider _odidoProvider = null!;
    private Carrier _kpnCarrier = null!;
    private Carrier _vodafoneCarrier = null!;

    public async Task InitializeAsync()
    {
        _context = fixture.CreateContext();

        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();

        // Seed required providers, bands, and carriers
        _kpnProvider = new Provider { ExternalId = AntennaGlobals.KpnProviderId, Name = "KPN" };
        _vodafoneProvider = new Provider { ExternalId = AntennaGlobals.VodafoneProviderId, Name = "Vodafone" };
        _odidoProvider = new Provider { ExternalId = AntennaGlobals.OdidoProviderId, Name = "Odido" };

        var band28 = new Band { ExternalId = 28, Name = "700MHz band 28" };
        var band20 = new Band { ExternalId = 20, Name = "800MHz band 20" };

        _context.Providers.AddRange(_kpnProvider, _vodafoneProvider, _odidoProvider);
        _context.Bands.AddRange(band28, band20);
        await _context.SaveChangesAsync();

        _kpnCarrier = new Carrier
        {
            ExternalId = 100,
            FrequencyLow = 768_000_000,
            FrequencyHigh = 778_000_000,
            ProviderId = _kpnProvider.Id,
            BandId = band28.Id
        };
        _vodafoneCarrier = new Carrier
        {
            ExternalId = 101,
            FrequencyLow = 758_000_000,
            FrequencyHigh = 768_000_000,
            ProviderId = _vodafoneProvider.Id,
            BandId = band28.Id
        };

        _context.Carriers.AddRange(_kpnCarrier, _vodafoneCarrier);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var baseSyncService = new BaseSyncService();
        var unitOfWork = new TestUnitOfWork(_context);
        var logger = NullLogger<BaseStationSyncService>.Instance;
        _baseStationSyncService = new BaseStationSyncService(unitOfWork, baseSyncService, logger);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task SyncBaseStationsAsync_AddUpdateDelete_AllOperationsWork()
    {
        var existingBs = new BaseStation
        {
            ExternalId = 1,
            Location = CreatePoint(5, 52),
            Municipality = "OldCity",
            PostalCode = "0000XX",
            City = "OldCity",
            IsSmallCell = false,
            ProviderId = _kpnProvider.Id
        };
        var deleteBs = new BaseStation
        {
            ExternalId = 2,
            Location = CreatePoint(4, 51),
            Municipality = "DeleteCity",
            PostalCode = "2222XX",
            City = "DeleteCity",
            IsSmallCell = false,
            ProviderId = _kpnProvider.Id
        };

        _context.BaseStations.AddRange(existingBs, deleteBs);
        await _context.SaveChangesAsync();

        _context.Antennas.Add(new Antenna
        {
            ExternalId = 101,
            Frequency = 773_000_000,
            Height = 30,
            Direction = 90,
            TransmissionPower = 100,
            IsDirectional = true,
            SatCode = "SAT001",
            BaseStationId = existingBs.Id,
            CarrierId = _kpnCarrier.Id
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101], CreatePoint(5.5, 52.5), "UpdatedCity", "1111AA", "UpdatedCity", true),
            new(3, [301], CreatePoint(6.0, 53.0), "NewCity", "3333AA", "NewCity", false)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] = [new AntenneRegisterAntenna(101, "SAT001", true, 30m, 90m, 100m, "773 MHz", null, null)],
            [3] = [new AntenneRegisterAntenna(301, "SAT003", false, 20m, 0m, 50m, "773 MHz", null, null)]
        };

        var result = await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        result.Basestations.Added.Should().Be(1);
        result.Basestations.Updated.Should().Be(1);
        result.Basestations.Deleted.Should().Be(1);

        var dbBaseStations = await _context.BaseStations.OrderBy(bs => bs.ExternalId).ToListAsync();
        dbBaseStations.Should().HaveCount(2);
        dbBaseStations[0].ExternalId.Should().Be(1);
        dbBaseStations[0].City.Should().Be("UpdatedCity");
        dbBaseStations[0].Location.X.Should().BeApproximately(5.5, 1e-9);
        dbBaseStations[0].Location.Y.Should().BeApproximately(52.5, 1e-9);

        dbBaseStations[1].ExternalId.Should().Be(3);
    }

    [Fact]
    public async Task SyncBaseStationsAsync_AntennaNotInIncoming_Deletes()
    {
        var bs = new BaseStation
        {
            ExternalId = 1,
            Location = CreatePoint(5, 52),
            Municipality = "Amsterdam",
            PostalCode = "1000AA",
            City = "Amsterdam",
            IsSmallCell = false,
            ProviderId = _kpnProvider.Id
        };
        _context.BaseStations.Add(bs);
        await _context.SaveChangesAsync();

        _context.Antennas.AddRange(
            new Antenna
            {
                ExternalId = 101, Frequency = 773_000_000, Height = 30, Direction = 90, TransmissionPower = 100,
                IsDirectional = true, SatCode = "SAT001", BaseStationId = bs.Id, CarrierId = _kpnCarrier.Id
            },
            new Antenna
            {
                ExternalId = 102, Frequency = 773_000_000, Height = 25, Direction = 180, TransmissionPower = 80,
                IsDirectional = false, SatCode = "SAT002", BaseStationId = bs.Id, CarrierId = _kpnCarrier.Id
            }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101], CreatePoint(5.0, 52.0), "Amsterdam", "1000AA", "Amsterdam", false)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] = [new AntenneRegisterAntenna(101, "SAT001", true, 30m, 90m, 100m, "773 MHz", null, null)]
        };

        var result = await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        result.Antennas.Deleted.Should().Be(1);

        var dbAntennas = await _context.Antennas.ToListAsync();
        dbAntennas.Should().HaveCount(1);
        dbAntennas[0].ExternalId.Should().Be(101);
    }

    [Fact]
    public async Task SyncBaseStationsAsync_ExistingAntenna_UpdatesProperties()
    {
        var bs = new BaseStation
        {
            ExternalId = 1,
            Location = CreatePoint(5, 52),
            Municipality = "Amsterdam",
            PostalCode = "1000AA",
            City = "Amsterdam",
            IsSmallCell = false,
            ProviderId = _kpnProvider.Id
        };
        _context.BaseStations.Add(bs);
        await _context.SaveChangesAsync();

        _context.Antennas.Add(new Antenna
        {
            ExternalId = 101,
            Frequency = 773_000_000,
            Height = 30,
            Direction = 90,
            TransmissionPower = 100,
            IsDirectional = true,
            SatCode = "OLD_SAT",
            BaseStationId = bs.Id,
            CarrierId = _kpnCarrier.Id
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101], CreatePoint(5.0, 52.0), "Amsterdam", "1000AA", "Amsterdam", false)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] = [new AntenneRegisterAntenna(101, "NEW_SAT", false, 50m, 180m, 200m, "773 MHz", null, null)]
        };

        var result = await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        result.Antennas.Updated.Should().Be(1);

        var dbAntenna = await _context.Antennas.FirstAsync();
        dbAntenna.SatCode.Should().Be("NEW_SAT");
        dbAntenna.IsDirectional.Should().BeFalse();
        dbAntenna.Height.Should().Be(50);
        dbAntenna.Direction.Should().Be(180);
        dbAntenna.TransmissionPower.Should().Be(200);
    }

    [Fact]
    public async Task SyncBaseStationsAsync_ExistingBaseStation_UpdatesProperties()
    {
        var bs = new BaseStation
        {
            ExternalId = 1,
            Location = CreatePoint(5, 52),
            Municipality = "OldMunicipality",
            PostalCode = "0000XX",
            City = "OldCity",
            IsSmallCell = false,
            ProviderId = _kpnProvider.Id
        };
        _context.BaseStations.Add(bs);
        await _context.SaveChangesAsync();

        _context.Antennas.Add(new Antenna
        {
            ExternalId = 101,
            Frequency = 773_000_000,
            Height = 30,
            Direction = 90,
            TransmissionPower = 100,
            IsDirectional = true,
            SatCode = "SAT001",
            BaseStationId = bs.Id,
            CarrierId = _kpnCarrier.Id
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101], CreatePoint(5.5, 52.5), "NewMunicipality", "1111AA", "NewCity", true)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] = [new AntenneRegisterAntenna(101, "SAT001", true, 30m, 90m, 100m, "773 MHz", null, null)]
        };

        var result = await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        result.Basestations.Updated.Should().Be(1);

        var dbBaseStation = await _context.BaseStations.FirstAsync();
        dbBaseStation.Municipality.Should().Be("NewMunicipality");
        dbBaseStation.PostalCode.Should().Be("1111AA");
        dbBaseStation.City.Should().Be("NewCity");
        dbBaseStation.IsSmallCell.Should().BeTrue();

        // You can finally test this now:
        dbBaseStation.Location.X.Should().BeApproximately(5.5, 1e-9);
        dbBaseStation.Location.Y.Should().BeApproximately(52.5, 1e-9);
    }

    private static Point CreatePoint(double longitude, double latitude)
    {
        return new Point(longitude, latitude) { SRID = 4326 };
    }
}