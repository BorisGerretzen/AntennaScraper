using AntennaScraper.Lib.Entities;
using Microsoft.EntityFrameworkCore;

namespace AntennaScraper.Lib.Persistence;

public class AntennaDbContext(DbContextOptions<AntennaDbContext> options) : DbContext(options)
{
    public DbSet<Antenna> Antennas { get; set; }
    public DbSet<Band> Bands { get; set; }
    public DbSet<BaseStation> BaseStations { get; set; }
    public DbSet<Carrier> Carriers { get; set; }
    public DbSet<Provider> Providers { get; set; }
    public DbSet<SyncLog> SyncLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AntennaDbContext).Assembly);

        foreach (var type in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IDefaultEntity).IsAssignableFrom(type.ClrType))
            {
                var entityBuilder = modelBuilder.Entity(type.ClrType);

                entityBuilder.HasKey(nameof(IDefaultEntity.Id));
                entityBuilder.Property(nameof(IDefaultEntity.Id)).IsRequired();

                if (!IsSqliteProvider())
                {
                    entityBuilder.Property(nameof(IDefaultEntity.RowVersion))
                        .IsRowVersion()
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();
                    entityBuilder.Property(nameof(IDefaultEntity.CreatedAt))
                        .IsRequired()
                        .HasDefaultValueSql("CURRENT_TIMESTAMP")
                        .ValueGeneratedOnAdd();
                    entityBuilder.Property(nameof(IDefaultEntity.UpdatedAt))
                        .IsRequired()
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");
                }
            }

            if (typeof(ISyncEntity).IsAssignableFrom(type.ClrType))
            {
                var entityBuilder = modelBuilder.Entity(type.ClrType);

                entityBuilder.Property(nameof(ISyncEntity.ExternalId)).IsRequired();
                entityBuilder.HasIndex(nameof(ISyncEntity.ExternalId)).IsUnique();
            }
        }
                    
        if (IsSqliteProvider())
        {
            var entityBuilder = modelBuilder.Entity<BaseStation>();
            entityBuilder.Ignore(nameof(BaseStation.Location));
            entityBuilder.Property(nameof(BaseStation.Latitude)).IsRequired();
            entityBuilder.Property(nameof(BaseStation.Longitude)).IsRequired();
        }
    }

    private void UpdateUpdatedAtProperties()
    {
        if (IsSqliteProvider()) return;
        var entries = ChangeTracker
            .Entries()
            .Where(e => e is { Entity: IDefaultEntity, State: EntityState.Added or EntityState.Modified });

        foreach (var entityEntry in entries) ((IDefaultEntity)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateUpdatedAtProperties();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new())
    {
        UpdateUpdatedAtProperties();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    
    private bool IsSqliteProvider()
    {
        return Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
    }
}