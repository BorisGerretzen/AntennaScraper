namespace AntennaScraper.Tests.Lib.Tests.Services.Sync;

public class BaseSyncServiceTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly BaseSyncService _syncService;

    public BaseSyncServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"DataSource={Guid.NewGuid()};Mode=Memory;Cache=Shared")
            .Options;

        _context = new TestDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _syncService = new BaseSyncService();
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public async Task SyncObjectsAsync_EmptyDatabase_AddsAllEntities()
    {
        // Arrange
        var incoming = new List<TestEntity>
        {
            new() { ExternalId = 1, Name = "Entity1", Value = 10 },
            new() { ExternalId = 2, Name = "Entity2", Value = 20 },
            new() { ExternalId = 3, Name = "Entity3", Value = 30 }
        };

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        result.Added.Should().Be(3);
        result.Updated.Should().Be(0);
        result.Deleted.Should().Be(0);

        var dbEntities = await _context.TestEntities.ToListAsync();
        dbEntities.Should().HaveCount(3);
        dbEntities.Select(e => e.ExternalId).Should().BeEquivalentTo([1L, 2L, 3L]);
    }

    [Fact]
    public async Task SyncObjectsAsync_PartiallyExistingEntities_AddsOnlyNewOnes()
    {
        // Arrange
        _context.TestEntities.Add(new TestEntity { ExternalId = 1, Name = "Existing" });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var incoming = new List<TestEntity>
        {
            new() { ExternalId = 1, Name = "Entity1" },
            new() { ExternalId = 2, Name = "Entity2" },
            new() { ExternalId = 3, Name = "Entity3" }
        };

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        result.Added.Should().Be(2);
        var dbEntities = await _context.TestEntities.ToListAsync();
        dbEntities.Should().HaveCount(3);
    }

    [Fact]
    public async Task SyncObjectsAsync_ExistingEntity_UpdatesSpecifiedColumns()
    {
        // Arrange
        _context.TestEntities.Add(new TestEntity { ExternalId = 1, Name = "OldName", Value = 10, IsActive = false });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var incoming = new List<TestEntity>
        {
            new() { ExternalId = 1, Name = "NewName", Value = 99, IsActive = false }
        };

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None,
            columnsToUpdate: [e => e.Name, e => e.Value]);
        await _context.SaveChangesAsync();

        // Assert
        result.Added.Should().Be(0);
        result.Updated.Should().Be(1);
        result.Deleted.Should().Be(0);

        var dbEntity = await _context.TestEntities.FirstAsync();
        dbEntity.Name.Should().Be("NewName");
        dbEntity.Value.Should().Be(99);
    }

    [Fact]
    public async Task SyncObjectsAsync_ExistingEntity_OnlyMarksSpecifiedColumnsAsModified()
    {
        // Arrange
        _context.TestEntities.Add(new TestEntity { ExternalId = 1, Name = "OldName", Value = 10, IsActive = false });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var incoming = new List<TestEntity>
        {
            new() { ExternalId = 1, Name = "NewName", Value = 99, IsActive = true }
        };

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None,
            columnsToUpdate: [e => e.Name, e => e.Value]);

        // Verify only specified columns are marked modified before save
        var entry = _context.Entry(incoming[0]);
        entry.Property(e => e.Name).IsModified.Should().BeTrue();
        entry.Property(e => e.Value).IsModified.Should().BeTrue();
        entry.Property(e => e.IsActive).IsModified.Should().BeFalse();

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Assert - The DB should only have updated Name and Value
        // IsActive gets updated because the attached entity had IsActive=true
        // but IsModified was false, so EF should not include it in the UPDATE
        result.Updated.Should().Be(1);

        var dbEntity = await _context.TestEntities.FirstAsync();
        dbEntity.Name.Should().Be("NewName");
        dbEntity.Value.Should().Be(99);
        // Note: IsActive will be whatever the attached entity had, but since IsModified=false,
        // EF Core should NOT update it in the database (the original value remains)
        dbEntity.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SyncObjectsAsync_ExistingEntityWithSameValues_DoesNotUpdate()
    {
        // Arrange
        _context.TestEntities.Add(new TestEntity { ExternalId = 1, Name = "SameName", Value = 10 });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var incoming = new List<TestEntity>
        {
            new() { ExternalId = 1, Name = "SameName", Value = 10 }
        };

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None,
            columnsToUpdate: [e => e.Name, e => e.Value]);
        await _context.SaveChangesAsync();

        // Assert
        result.Updated.Should().Be(0);
    }

    [Fact]
    public async Task SyncObjectsAsync_NoColumnsToUpdate_DoesNotUpdateExisting()
    {
        // Arrange
        _context.TestEntities.Add(new TestEntity { ExternalId = 1, Name = "OldName", Value = 10 });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var incoming = new List<TestEntity>
        {
            new() { ExternalId = 1, Name = "NewName", Value = 99 }
        };

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        result.Updated.Should().Be(0);

        var dbEntity = await _context.TestEntities.FirstAsync();
        dbEntity.Name.Should().Be("OldName"); // Should remain unchanged
        dbEntity.Value.Should().Be(10);
    }

    [Fact]
    public async Task SyncObjectsAsync_EntityNotInIncoming_DeletesFromDatabase()
    {
        // Arrange
        _context.TestEntities.AddRange(
            new TestEntity { ExternalId = 1, Name = "Keep" },
            new TestEntity { ExternalId = 2, Name = "Delete" },
            new TestEntity { ExternalId = 3, Name = "AlsoDelete" }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var incoming = new List<TestEntity>
        {
            new() { ExternalId = 1, Name = "Keep" }
        };

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        result.Deleted.Should().Be(2);

        var dbEntities = await _context.TestEntities.ToListAsync();
        dbEntities.Should().HaveCount(1);
        dbEntities[0].ExternalId.Should().Be(1);
    }

    [Fact]
    public async Task SyncObjectsAsync_EmptyIncoming_DeletesAllFromDatabase()
    {
        // Arrange
        _context.TestEntities.AddRange(
            new TestEntity { ExternalId = 1, Name = "Delete1" },
            new TestEntity { ExternalId = 2, Name = "Delete2" }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var incoming = new List<TestEntity>();

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        result.Deleted.Should().Be(2);

        var dbEntities = await _context.TestEntities.ToListAsync();
        dbEntities.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncObjectsAsync_WithAdditionalDeleteCondition_OnlyDeletesMatchingEntities()
    {
        // Arrange
        _context.TestEntities.AddRange(
            new TestEntity { ExternalId = 1, Name = "Keep", IsActive = true },
            new TestEntity { ExternalId = 2, Name = "DeleteMe", IsActive = true },
            new TestEntity { ExternalId = 3, Name = "Protected", IsActive = false }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var incoming = new List<TestEntity>
        {
            new() { ExternalId = 1, Name = "Keep" }
        };

        // Act - Only delete active entities not in incoming
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None,
            e => e.IsActive);
        await _context.SaveChangesAsync();

        // Assert
        result.Deleted.Should().Be(1); // Only ExternalId=2 should be deleted

        var dbEntities = await _context.TestEntities.ToListAsync();
        dbEntities.Should().HaveCount(2);
        dbEntities.Select(e => e.ExternalId).Should().BeEquivalentTo([1L, 3L]);
    }

    [Fact]
    public async Task SyncObjectsAsync_DuplicateExternalIdsInIncoming_UsesFirstOccurrence()
    {
        // Arrange
        var incoming = new List<TestEntity>
        {
            new() { ExternalId = 1, Name = "First", Value = 100 },
            new() { ExternalId = 1, Name = "Duplicate", Value = 999 },
            new() { ExternalId = 2, Name = "Second", Value = 200 }
        };

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        result.Added.Should().Be(2);

        var dbEntities = await _context.TestEntities.ToListAsync();
        dbEntities.Should().HaveCount(2);

        var entity1 = dbEntities.First(e => e.ExternalId == 1);
        entity1.Name.Should().Be("First");
        entity1.Value.Should().Be(100);
    }

    [Fact]
    public async Task SyncObjectsAsync_MoreThanBatchSize_ProcessesAllEntities()
    {
        // Arrange - Create 2500 entities (more than batch size of 1000)
        var incoming = Enumerable.Range(1, 2500)
            .Select(i => new TestEntity { ExternalId = i, Name = $"Entity{i}", Value = i })
            .ToList();

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        result.Added.Should().Be(2500);
        result.Updated.Should().Be(0);
        result.Deleted.Should().Be(0);

        var count = await _context.TestEntities.CountAsync();
        count.Should().Be(2500);
    }

    [Fact]
    public async Task SyncObjectsAsync_LargeBatchWithUpdates_UpdatesCorrectly()
    {
        // Arrange - Seed 1500 entities
        var existingEntities = Enumerable.Range(1, 1500)
            .Select(i => new TestEntity { ExternalId = i, Name = $"Old{i}", Value = i })
            .ToList();
        _context.TestEntities.AddRange(existingEntities);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Incoming: update first 1000, delete next 500
        var incoming = Enumerable.Range(1, 1000)
            .Select(i => new TestEntity { ExternalId = i, Name = $"New{i}", Value = i * 10 })
            .ToList();

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None,
            columnsToUpdate: [e => e.Name, e => e.Value]);
        await _context.SaveChangesAsync();

        // Assert
        result.Added.Should().Be(0);
        result.Updated.Should().Be(1000);
        result.Deleted.Should().Be(500);

        var count = await _context.TestEntities.CountAsync();
        count.Should().Be(1000);

        var firstEntity = await _context.TestEntities.FirstAsync(e => e.ExternalId == 1);
        firstEntity.Name.Should().Be("New1");
        firstEntity.Value.Should().Be(10);
    }

    [Fact]
    public async Task SyncObjectsAsync_AddUpdateAndDelete_AllOperationsWork()
    {
        // Arrange
        _context.TestEntities.AddRange(
            new TestEntity { ExternalId = 1, Name = "Update", Value = 10 },
            new TestEntity { ExternalId = 2, Name = "Delete", Value = 20 }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var incoming = new List<TestEntity>
        {
            new() { ExternalId = 1, Name = "Updated", Value = 100 },
            new() { ExternalId = 3, Name = "New", Value = 30 }
        };

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None,
            columnsToUpdate: [e => e.Name, e => e.Value]);
        await _context.SaveChangesAsync();

        // Assert
        result.Added.Should().Be(1);
        result.Updated.Should().Be(1);
        result.Deleted.Should().Be(1);

        var dbEntities = await _context.TestEntities.OrderBy(e => e.ExternalId).ToListAsync();
        dbEntities.Should().HaveCount(2);

        dbEntities[0].ExternalId.Should().Be(1);
        dbEntities[0].Name.Should().Be("Updated");
        dbEntities[0].Value.Should().Be(100);

        dbEntities[1].ExternalId.Should().Be(3);
        dbEntities[1].Name.Should().Be("New");
    }

    [Fact]
    public async Task SyncObjectsAsync_EmptyDatabaseEmptyIncoming_ReturnsZeroCounts()
    {
        // Arrange
        var incoming = new List<TestEntity>();

        // Act
        var result = await _syncService.SyncObjectsAsync(
            incoming,
            _context.TestEntities,
            CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        result.Added.Should().Be(0);
        result.Updated.Should().Be(0);
        result.Deleted.Should().Be(0);
    }

    [Fact]
    public async Task SyncObjectsAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var incoming = Enumerable.Range(1, 100)
            .Select(i => new TestEntity { ExternalId = i, Name = $"Entity{i}" })
            .ToList();

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _syncService.SyncObjectsAsync(
                incoming,
                _context.TestEntities,
                cts.Token));
    }

    private class TestEntity : ISyncEntity
    {
        public int Id { get; set; }
        public uint RowVersion { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long ExternalId { get; set; }

        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool IsActive { get; set; }
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ExternalId).IsUnique();
            });
        }
    }
}