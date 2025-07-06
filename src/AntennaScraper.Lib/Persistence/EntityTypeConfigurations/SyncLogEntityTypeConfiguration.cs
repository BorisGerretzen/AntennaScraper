using AntennaScraper.Lib.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntennaScraper.Lib.Persistence.EntityTypeConfigurations;

public class SyncLogEntityTypeConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> builder)
    {
        builder.Property(sl => sl.SyncStartedAt).IsRequired();
        builder.Property(sl => sl.SyncEndedAt).IsRequired();
        builder.Property(sl => sl.IsSuccessful).IsRequired();
    }
}