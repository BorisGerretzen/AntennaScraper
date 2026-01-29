using AntennaScraper.Lib.Services.Data.CarrierService;
using AntennaScraper.Lib.Services.Sync.CarrierSyncService;

namespace AntennaScraper.Tests.Lib.Tests.Services.Sync;

public class CarrierSyncServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();
    private readonly AntennaDbContext _context;
    private readonly CarrierSyncService _carrierSyncService;
    private readonly Provider _kpnProvider;
    private readonly Provider _vodafoneProvider;
    private readonly Band _band28;
    private readonly Band _band20;

    public CarrierSyncServiceTests()
    {
        _context = _fixture.CreateContext();

        // Seed required providers and bands
        _kpnProvider = new Provider { ExternalId = 1, Name = "KPN" };
        _vodafoneProvider = new Provider { ExternalId = 4, Name = "Vodafone" };
        _band28 = new Band { ExternalId = 28, Name = "700MHz band 28" };
        _band20 = new Band { ExternalId = 20, Name = "800MHz band 20" };

        _context.Providers.AddRange(_kpnProvider, _vodafoneProvider);
        _context.Bands.AddRange(_band28, _band20);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        var baseSyncService = new BaseSyncService();
        var unitOfWork = SqliteFixture.CreateUnitOfWork(_context);
        _carrierSyncService = new CarrierSyncService(unitOfWork, baseSyncService);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }


    [Fact]
    public async Task SyncCarriersAsync_EmptyDatabase_AddsAllCarriers()
    {
        // Arrange
        var carriers = new List<CarrierDto>
        {
            new(100, 1, 758_000_000, 768_000_000, 28),
            new(101, 4, 791_000_000, 801_000_000, 20)
        };

        // Act
        var result = await _carrierSyncService.SyncCarriersAsync(carriers, CancellationToken.None);

        // Assert
        result.Added.Should().Be(2);
        result.Updated.Should().Be(0);
        result.Deleted.Should().Be(0);

        var dbCarriers = await _context.Carriers.ToListAsync();
        dbCarriers.Should().HaveCount(2);
        dbCarriers.Select(c => c.ExternalId).Should().BeEquivalentTo([100L, 101L]);
    }

    [Fact]
    public async Task SyncCarriersAsync_PartiallyExistingCarriers_AddsOnlyNewOnes()
    {
        // Arrange - Seed one carrier
        _context.Carriers.Add(new Carrier
        {
            ExternalId = 100,
            FrequencyLow = 758_000_000,
            FrequencyHigh = 768_000_000,
            ProviderId = _kpnProvider.Id,
            BandId = _band28.Id
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var carriers = new List<CarrierDto>
        {
            new(100, 1, 758_000_000, 768_000_000, 28),
            new(101, 4, 791_000_000, 801_000_000, 20)
        };

        // Act
        var result = await _carrierSyncService.SyncCarriersAsync(carriers, CancellationToken.None);

        // Assert
        result.Added.Should().Be(1);
        var dbCarriers = await _context.Carriers.ToListAsync();
        dbCarriers.Should().HaveCount(2);
    }


    [Fact]
    public async Task SyncCarriersAsync_ExistingCarrier_UpdatesFrequencyAndMappings()
    {
        // Arrange
        _context.Carriers.Add(new Carrier
        {
            ExternalId = 100,
            FrequencyLow = 700_000_000,
            FrequencyHigh = 710_000_000,
            ProviderId = _kpnProvider.Id,
            BandId = _band28.Id
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var carriers = new List<CarrierDto>
        {
            new(100, 4, 758_000_000, 768_000_000, 20) // Changed provider, frequencies, and band
        };

        // Act
        var result = await _carrierSyncService.SyncCarriersAsync(carriers, CancellationToken.None);

        // Assert
        result.Updated.Should().Be(1);

        var dbCarrier = await _context.Carriers.FirstAsync();
        dbCarrier.FrequencyLow.Should().Be(758_000_000);
        dbCarrier.FrequencyHigh.Should().Be(768_000_000);
        dbCarrier.ProviderId.Should().Be(_vodafoneProvider.Id);
        dbCarrier.BandId.Should().Be(_band20.Id);
    }

    [Fact]
    public async Task SyncCarriersAsync_ExistingCarrierWithSameValues_DoesNotUpdate()
    {
        // Arrange
        _context.Carriers.Add(new Carrier
        {
            ExternalId = 100,
            FrequencyLow = 758_000_000,
            FrequencyHigh = 768_000_000,
            ProviderId = _kpnProvider.Id,
            BandId = _band28.Id
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var carriers = new List<CarrierDto>
        {
            new(100, 1, 758_000_000, 768_000_000, 28) // Same values
        };

        // Act
        var result = await _carrierSyncService.SyncCarriersAsync(carriers, CancellationToken.None);

        // Assert
        result.Updated.Should().Be(0);
    }


    [Fact]
    public async Task SyncCarriersAsync_CarrierNotInIncoming_DeletesFromDatabase()
    {
        // Arrange
        _context.Carriers.AddRange(
            new Carrier { ExternalId = 100, FrequencyLow = 758_000_000, FrequencyHigh = 768_000_000, ProviderId = _kpnProvider.Id, BandId = _band28.Id },
            new Carrier { ExternalId = 101, FrequencyLow = 791_000_000, FrequencyHigh = 801_000_000, ProviderId = _vodafoneProvider.Id, BandId = _band20.Id }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var carriers = new List<CarrierDto>
        {
            new(100, 1, 758_000_000, 768_000_000, 28)
        };

        // Act
        var result = await _carrierSyncService.SyncCarriersAsync(carriers, CancellationToken.None);

        // Assert
        result.Deleted.Should().Be(1);
        var dbCarriers = await _context.Carriers.ToListAsync();
        dbCarriers.Should().HaveCount(1);
        dbCarriers[0].ExternalId.Should().Be(100);
    }


    [Fact]
    public async Task SyncCarriersAsync_InvalidProviderId_ThrowsInvalidOperationException()
    {
        // Arrange
        var carriers = new List<CarrierDto>
        {
            new(100, 999, 758_000_000, 768_000_000, 28) // Provider 999 doesn't exist
        };

        // Act
        var act = () => _carrierSyncService.SyncCarriersAsync(carriers, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid provider or band*");
    }

    [Fact]
    public async Task SyncCarriersAsync_InvalidBandId_ThrowsInvalidOperationException()
    {
        // Arrange
        var carriers = new List<CarrierDto>
        {
            new(100, 1, 758_000_000, 768_000_000, 999) // Band 999 doesn't exist
        };

        // Act
        var act = () => _carrierSyncService.SyncCarriersAsync(carriers, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid provider or band*");
    }


    [Fact]
    public async Task SyncCarriersAsync_MapsExternalIdsToInternalIds()
    {
        // Arrange
        var carriers = new List<CarrierDto>
        {
            new(100, 1, 758_000_000, 768_000_000, 28) // External provider ID 1, band ID 28
        };

        // Act
        await _carrierSyncService.SyncCarriersAsync(carriers, CancellationToken.None);

        // Assert
        var dbCarrier = await _context.Carriers.FirstAsync();
        dbCarrier.ProviderId.Should().Be(_kpnProvider.Id); // Internal ID
        dbCarrier.BandId.Should().Be(_band28.Id); // Internal ID
    }

    [Fact]
    public async Task SyncCarriersAsync_DuplicateIds_UsesFirstOccurrence()
    {
        // Arrange
        var carriers = new List<CarrierDto>
        {
            new(100, 1, 758_000_000, 768_000_000, 28),
            new(100, 4, 791_000_000, 801_000_000, 20) // Duplicate ID
        };

        // Act
        var result = await _carrierSyncService.SyncCarriersAsync(carriers, CancellationToken.None);

        // Assert
        result.Added.Should().Be(1);

        var dbCarrier = await _context.Carriers.FirstAsync();
        dbCarrier.ProviderId.Should().Be(_kpnProvider.Id); // First occurrence used
        dbCarrier.FrequencyLow.Should().Be(758_000_000);
    }


    [Fact]
    public async Task SyncCarriersAsync_AddUpdateDelete_AllOperationsWork()
    {
        // Arrange
        _context.Carriers.AddRange(
            new Carrier { ExternalId = 100, FrequencyLow = 700_000_000, FrequencyHigh = 710_000_000, ProviderId = _kpnProvider.Id, BandId = _band28.Id },
            new Carrier { ExternalId = 101, FrequencyLow = 791_000_000, FrequencyHigh = 801_000_000, ProviderId = _vodafoneProvider.Id, BandId = _band20.Id }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var carriers = new List<CarrierDto>
        {
            new(100, 4, 758_000_000, 768_000_000, 20), // Update
            new(102, 1, 925_000_000, 935_000_000, 28) // Add
            // 101 is deleted
        };

        // Act
        var result = await _carrierSyncService.SyncCarriersAsync(carriers, CancellationToken.None);

        // Assert
        result.Added.Should().Be(1);
        result.Updated.Should().Be(1);
        result.Deleted.Should().Be(1);

        var dbCarriers = await _context.Carriers.OrderBy(c => c.ExternalId).ToListAsync();
        dbCarriers.Should().HaveCount(2);
        dbCarriers[0].ExternalId.Should().Be(100);
        dbCarriers[1].ExternalId.Should().Be(102);
    }

    [Fact]
    public async Task SyncCarriersAsync_EmptyIncoming_DeletesAll()
    {
        // Arrange
        _context.Carriers.AddRange(
            new Carrier { ExternalId = 100, FrequencyLow = 758_000_000, FrequencyHigh = 768_000_000, ProviderId = _kpnProvider.Id, BandId = _band28.Id },
            new Carrier { ExternalId = 101, FrequencyLow = 791_000_000, FrequencyHigh = 801_000_000, ProviderId = _vodafoneProvider.Id, BandId = _band20.Id }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var carriers = new List<CarrierDto>();

        // Act
        var result = await _carrierSyncService.SyncCarriersAsync(carriers, CancellationToken.None);

        // Assert
        result.Deleted.Should().Be(2);
        var dbCarriers = await _context.Carriers.ToListAsync();
        dbCarriers.Should().BeEmpty();
    }
}