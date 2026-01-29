using AntennaScraper.Lib.Services.Data.BandService;
using AntennaScraper.Lib.Services.Sync.BandSyncService;

namespace AntennaScraper.Tests.Lib.Tests.Services.Sync;

public class BandSyncServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();
    private readonly AntennaDbContext _context;
    private readonly BandSyncService _bandSyncService;

    public BandSyncServiceTests()
    {
        _context = _fixture.CreateContext();

        var baseSyncService = new BaseSyncService();
        var unitOfWork = SqliteFixture.CreateUnitOfWork(_context);
        _bandSyncService = new BandSyncService(unitOfWork, baseSyncService);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }


    [Fact]
    public async Task SyncBandsAsync_EmptyDatabase_AddsAllBands()
    {
        // Arrange
        var bands = new List<BandDto>
        {
            new(1, "700MHz band 28", "LTE band"),
            new(2, "800MHz band 20", "Another band"),
            new(3, "1800MHz band 3", "Third band")
        };

        // Act
        var result = await _bandSyncService.SyncBandsAsync(bands, CancellationToken.None);

        // Assert
        result.Added.Should().Be(3);
        result.Updated.Should().Be(0);
        result.Deleted.Should().Be(0);

        var dbBands = await _context.Bands.ToListAsync();
        dbBands.Should().HaveCount(3);
        dbBands.Select(b => b.ExternalId).Should().BeEquivalentTo([1L, 2L, 3L]);
        dbBands.Select(b => b.Name).Should().BeEquivalentTo(["700MHz band 28", "800MHz band 20", "1800MHz band 3"]);
    }

    [Fact]
    public async Task SyncBandsAsync_PartiallyExistingBands_AddsOnlyNewOnes()
    {
        // Arrange - Seed one band
        _context.Bands.Add(new Band { ExternalId = 1, Name = "Existing Band", Description = "Desc" });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var bands = new List<BandDto>
        {
            new(1, "700MHz band 28", "LTE band"),
            new(2, "800MHz band 20", "Another band"),
            new(3, "1800MHz band 3", "Third band")
        };

        // Act
        var result = await _bandSyncService.SyncBandsAsync(bands, CancellationToken.None);

        // Assert
        result.Added.Should().Be(2);
        var dbBands = await _context.Bands.ToListAsync();
        dbBands.Should().HaveCount(3);
    }


    [Fact]
    public async Task SyncBandsAsync_ExistingBand_UpdatesNameAndDescription()
    {
        // Arrange
        _context.Bands.Add(new Band { ExternalId = 1, Name = "OldName", Description = "OldDesc" });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var bands = new List<BandDto>
        {
            new(1, "NewName", "NewDesc")
        };

        // Act
        var result = await _bandSyncService.SyncBandsAsync(bands, CancellationToken.None);

        // Assert
        result.Updated.Should().Be(1);
        result.Added.Should().Be(0);
        result.Deleted.Should().Be(0);

        var dbBand = await _context.Bands.FirstAsync();
        dbBand.Name.Should().Be("NewName");
        dbBand.Description.Should().Be("NewDesc");
    }

    [Fact]
    public async Task SyncBandsAsync_ExistingBandWithSameValues_DoesNotUpdate()
    {
        // Arrange
        _context.Bands.Add(new Band { ExternalId = 1, Name = "SameName", Description = "SameDesc" });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var bands = new List<BandDto>
        {
            new(1, "SameName", "SameDesc")
        };

        // Act
        var result = await _bandSyncService.SyncBandsAsync(bands, CancellationToken.None);

        // Assert
        result.Updated.Should().Be(0);
    }


    [Fact]
    public async Task SyncBandsAsync_BandNotInIncoming_DeletesFromDatabase()
    {
        // Arrange
        _context.Bands.AddRange(
            new Band { ExternalId = 1, Name = "Keep" },
            new Band { ExternalId = 2, Name = "Delete" },
            new Band { ExternalId = 3, Name = "AlsoDelete" }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var bands = new List<BandDto>
        {
            new(1, "Keep", "Desc")
        };

        // Act
        var result = await _bandSyncService.SyncBandsAsync(bands, CancellationToken.None);

        // Assert
        result.Deleted.Should().Be(2);

        var dbBands = await _context.Bands.ToListAsync();
        dbBands.Should().HaveCount(1);
        dbBands[0].ExternalId.Should().Be(1);
    }


    [Fact]
    public async Task SyncBandsAsync_EmptyAlias_FiltersOutBand()
    {
        // Arrange
        var bands = new List<BandDto>
        {
            new(1, "ValidBand", "Desc"),
            new(2, "", "EmptyAlias"),
            new(3, "   ", "WhitespaceAlias"),
            new(4, "AnotherValid", "Desc")
        };

        // Act
        var result = await _bandSyncService.SyncBandsAsync(bands, CancellationToken.None);

        // Assert
        result.Added.Should().Be(2);

        var dbBands = await _context.Bands.ToListAsync();
        dbBands.Should().HaveCount(2);
        dbBands.Select(b => b.ExternalId).Should().BeEquivalentTo([1L, 4L]);
    }

    [Fact]
    public async Task SyncBandsAsync_DuplicateIds_UsesFirstOccurrence()
    {
        // Arrange
        var bands = new List<BandDto>
        {
            new(1, "First", "First desc"),
            new(1, "Duplicate", "Duplicate desc"),
            new(2, "Second", "Second desc")
        };

        // Act
        var result = await _bandSyncService.SyncBandsAsync(bands, CancellationToken.None);

        // Assert
        result.Added.Should().Be(2);

        var band1 = await _context.Bands.FirstAsync(b => b.ExternalId == 1);
        band1.Name.Should().Be("First");
        band1.Description.Should().Be("First desc");
    }


    [Fact]
    public async Task SyncBandsAsync_AddUpdateAndDelete_AllOperationsWork()
    {
        // Arrange
        _context.Bands.AddRange(
            new Band { ExternalId = 1, Name = "Update", Description = "Old" },
            new Band { ExternalId = 2, Name = "Delete", Description = "Delete" }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var bands = new List<BandDto>
        {
            new(1, "Updated", "New"),
            new(3, "New", "Brand new")
        };

        // Act
        var result = await _bandSyncService.SyncBandsAsync(bands, CancellationToken.None);

        // Assert
        result.Added.Should().Be(1);
        result.Updated.Should().Be(1);
        result.Deleted.Should().Be(1);

        var dbBands = await _context.Bands.OrderBy(b => b.ExternalId).ToListAsync();
        dbBands.Should().HaveCount(2);
        dbBands[0].Name.Should().Be("Updated");
        dbBands[1].Name.Should().Be("New");
    }

    [Fact]
    public async Task SyncBandsAsync_EmptyIncoming_DeletesAll()
    {
        // Arrange
        _context.Bands.AddRange(
            new Band { ExternalId = 1, Name = "Delete1" },
            new Band { ExternalId = 2, Name = "Delete2" }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var bands = new List<BandDto>();

        // Act
        var result = await _bandSyncService.SyncBandsAsync(bands, CancellationToken.None);

        // Assert
        result.Deleted.Should().Be(2);
        var dbBands = await _context.Bands.ToListAsync();
        dbBands.Should().BeEmpty();
    }
}