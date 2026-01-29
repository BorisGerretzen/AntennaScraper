using AntennaScraper.Lib;
using AntennaScraper.Lib.Services.External.AntenneRegisterClient.Dto;
using AntennaScraper.Lib.Services.Sync.BaseStationSyncService;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;

namespace AntennaScraper.Tests.Lib.Tests.Services.Sync;

public class BaseStationSyncServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();
    private readonly AntennaDbContext _context;
    private readonly BaseStationSyncService _baseStationSyncService;
    private readonly Provider _kpnProvider;
    private readonly Provider _vodafoneProvider;
    private readonly Provider _odidoProvider;
    private readonly Carrier _kpnCarrier;
    private readonly Carrier _vodafoneCarrier;

    public BaseStationSyncServiceTests()
    {
        _context = _fixture.CreateContext();

        // Seed required providers, bands, and carriers
        _kpnProvider = new Provider { ExternalId = AntennaGlobals.KpnProviderId, Name = "KPN" };
        _vodafoneProvider = new Provider { ExternalId = AntennaGlobals.VodafoneProviderId, Name = "Vodafone" };
        _odidoProvider = new Provider { ExternalId = AntennaGlobals.OdidoProviderId, Name = "Odido" };
        var band28 = new Band { ExternalId = 28, Name = "700MHz band 28" };
        var band20 = new Band { ExternalId = 20, Name = "800MHz band 20" };

        _context.Providers.AddRange(_kpnProvider, _vodafoneProvider, _odidoProvider);
        _context.Bands.AddRange(band28, band20);
        _context.SaveChanges();

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
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        var baseSyncService = new BaseSyncService();
        var unitOfWork = SqliteFixture.CreateUnitOfWork(_context);
        var logger = NullLogger<BaseStationSyncService>.Instance;
        _baseStationSyncService = new BaseStationSyncService(unitOfWork, baseSyncService, logger);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private static Point CreatePoint(double x, double y)
    {
        return new Point(x, y) { SRID = 4326 };
    }

    [Fact]
    public async Task SyncBaseStationsAsync_EmptyDatabase_AddsBaseStationAndAntennas()
    {
        // Arrange
        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101, 102], CreatePoint(5.0, 52.0), "Amsterdam", "1000AA", "Amsterdam", false)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] =
            [
                new AntenneRegisterAntenna(101, "SAT001", true, 30m, 90m, 100m, "773 MHz", DateOnly.FromDateTime(DateTime.Today), null),
                new AntenneRegisterAntenna(102, "SAT002", false, 25m, 180m, 80m, "773 MHz", null, DateOnly.FromDateTime(DateTime.Today))
            ]
        };

        // Act
        var result = await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        // Assert
        result.Basestations.Added.Should().Be(1);
        result.Antennas.Added.Should().Be(2);

        var dbBaseStations = await _context.BaseStations.ToListAsync();
        dbBaseStations.Should().HaveCount(1);
        dbBaseStations[0].ExternalId.Should().Be(1);
        dbBaseStations[0].City.Should().Be("Amsterdam");
        dbBaseStations[0].ProviderId.Should().Be(_kpnProvider.Id);

        var dbAntennas = await _context.Antennas.ToListAsync();
        dbAntennas.Should().HaveCount(2);
        dbAntennas.Should().AllSatisfy(a => a.CarrierId.Should().Be(_kpnCarrier.Id));
    }

    [Fact]
    public async Task SyncBaseStationsAsync_MultipleBaseStations_AddsAll()
    {
        // Arrange
        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101], CreatePoint(5.0, 52.0), "Amsterdam", "1000AA", "Amsterdam", false),
            new(2, [201], CreatePoint(4.5, 52.5), "Rotterdam", "3000AA", "Rotterdam", true)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] = [new AntenneRegisterAntenna(101, "SAT001", true, 30m, 90m, 100m, "773 MHz", null, null)],
            [2] = [new AntenneRegisterAntenna(201, "SAT002", false, 20m, 0m, 50m, "773 MHz", null, null)]
        };

        // Act
        var result = await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        // Assert
        result.Basestations.Added.Should().Be(2);
        result.Antennas.Added.Should().Be(2);

        var dbBaseStations = await _context.BaseStations.ToListAsync();
        dbBaseStations.Should().HaveCount(2);
    }


    [Fact]
    public async Task SyncBaseStationsAsync_BaseStationWithNoAntennas_SkipsBaseStation()
    {
        // Arrange
        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [], CreatePoint(5.0, 52.0), "Amsterdam", "1000AA", "Amsterdam", false)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>();

        // Act
        var result = await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        // Assert
        result.Basestations.Added.Should().Be(0);
        result.Antennas.Added.Should().Be(0);

        var dbBaseStations = await _context.BaseStations.ToListAsync();
        dbBaseStations.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncBaseStationsAsync_BaseStationWithEmptyAntennaList_SkipsBaseStation()
    {
        // Arrange
        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101], CreatePoint(5.0, 52.0), "Amsterdam", "1000AA", "Amsterdam", false)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] = [] // Empty list
        };

        // Act
        var result = await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        // Assert
        result.Basestations.Added.Should().Be(0);
    }

    [Fact]
    public async Task SyncBaseStationsAsync_AntennaWithNoMatchingCarrier_SkipsAntenna()
    {
        // Arrange
        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101, 102], CreatePoint(5.0, 52.0), "Amsterdam", "1000AA", "Amsterdam", false)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] =
            [
                new AntenneRegisterAntenna(101, "SAT001", true, 30m, 90m, 100m, "773 MHz", null, null), // Valid frequency
                new AntenneRegisterAntenna(102, "SAT002", false, 25m, 180m, 80m, "9999 MHz", null, null) // Invalid frequency - no carrier
            ]
        };

        // Act
        var result = await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        // Assert
        result.Basestations.Added.Should().Be(1);
        result.Antennas.Added.Should().Be(1); // Only one antenna added

        var dbAntennas = await _context.Antennas.ToListAsync();
        dbAntennas.Should().HaveCount(1);
        dbAntennas[0].ExternalId.Should().Be(101);
    }

    [Fact]
    public async Task SyncBaseStationsAsync_MultipleProvidersInAntennas_SkipsBaseStation()
    {
        // Arrange - Base station with antennas from different providers
        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101, 102], CreatePoint(5.0, 52.0), "Amsterdam", "1000AA", "Amsterdam", false)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] =
            [
                new AntenneRegisterAntenna(101, "SAT001", true, 30m, 90m, 100m, "773 MHz", null, null), // KPN frequency
                new AntenneRegisterAntenna(102, "SAT002", false, 25m, 180m, 80m, "763 MHz", null, null) // Vodafone frequency
            ]
        };

        // Act
        var result = await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        // Assert - Should skip due to multiple providers
        result.Basestations.Added.Should().Be(0);
        result.Antennas.Added.Should().Be(0);
    }

    [Fact]
    public async Task SyncBaseStationsAsync_BaseStationNotInIncoming_Deletes()
    {
        // Arrange - Seed existing base station
        var bs = new BaseStation
        {
            ExternalId = 1,
            Location = null!, // Ignored in SQLite
            Longitude = 5.0,
            Latitude = 52.0,
            Municipality = "Amsterdam",
            PostalCode = "1000AA",
            City = "Amsterdam",
            IsSmallCell = false,
            ProviderId = _kpnProvider.Id
        };
        _context.BaseStations.Add(bs);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act - Empty incoming
        var result = await _baseStationSyncService.SyncBaseStationsAsync(
            new List<AntenneRegisterBaseStation>(),
            new Dictionary<long, List<AntenneRegisterAntenna>>(),
            CancellationToken.None);

        // Assert
        result.Basestations.Deleted.Should().Be(1);

        var dbBaseStations = await _context.BaseStations.ToListAsync();
        dbBaseStations.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncBaseStationsAsync_NoCarriersInDatabase_ThrowsInvalidOperationException()
    {
        // Arrange - Remove all carriers
        await _context.Carriers.ExecuteDeleteAsync();
        _context.ChangeTracker.Clear();

        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101], CreatePoint(5.0, 52.0), "Amsterdam", "1000AA", "Amsterdam", false)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] = [new AntenneRegisterAntenna(101, "SAT001", true, 30m, 90m, 100m, "773 MHz", null, null)]
        };

        // Act
        var act = () => _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No carriers found*");
    }


    [Fact]
    public async Task SyncBaseStationsAsync_ParsesFrequencyCorrectly()
    {
        // Arrange
        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101], CreatePoint(5.0, 52.0), "Amsterdam", "1000AA", "Amsterdam", false)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] = [new AntenneRegisterAntenna(101, "SAT001", true, 30m, 90m, 100m, "773 MHz", null, null)]
        };

        // Act
        await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        // Assert
        var dbAntenna = await _context.Antennas.FirstAsync();
        dbAntenna.Frequency.Should().Be(773_000_000); // 773 MHz in Hz
    }

    [Fact]
    public async Task SyncBaseStationsAsync_FrequencyRange_ParsesAverage()
    {
        // Arrange
        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101], CreatePoint(5.0, 52.0), "Amsterdam", "1000AA", "Amsterdam", false)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] = [new AntenneRegisterAntenna(101, "SAT001", true, 30m, 90m, 100m, "768-778 MHz", null, null)] // Range, average = 773
        };

        // Act
        await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        // Assert
        var dbAntenna = await _context.Antennas.FirstAsync();
        dbAntenna.Frequency.Should().Be(773_000_000);
    }

    [Fact]
    public async Task SyncBaseStationsAsync_DateFields_ArePreserved()
    {
        // Arrange
        var commissionDate = new DateOnly(2020, 5, 15);
        var changedDate = new DateOnly(2024, 1, 10);

        var baseStations = new List<AntenneRegisterBaseStation>
        {
            new(1, [101], CreatePoint(5.0, 52.0), "Amsterdam", "1000AA", "Amsterdam", false)
        };

        var antennasByBs = new Dictionary<long, List<AntenneRegisterAntenna>>
        {
            [1] = [new AntenneRegisterAntenna(101, "SAT001", true, 30m, 90m, 100m, "773 MHz", commissionDate, changedDate)]
        };

        // Act
        await _baseStationSyncService.SyncBaseStationsAsync(baseStations, antennasByBs, CancellationToken.None);

        // Assert
        var dbAntenna = await _context.Antennas.FirstAsync();
        dbAntenna.DateOfCommissioning.Should().Be(commissionDate);
        dbAntenna.DateLastChanged.Should().Be(changedDate);
    }
}