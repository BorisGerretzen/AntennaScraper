using AntennaScraper.Lib.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntennaScraper.Lib.Persistence.EntityTypeConfigurations;

public class BaseStationEntityTypeConfiguration : IEntityTypeConfiguration<BaseStation>
{
    public void Configure(EntityTypeBuilder<BaseStation> builder)
    {
        builder.Property(b => b.Location).IsRequired().HasColumnType("geometry(Point,4326)");
        builder.Ignore(b => b.Latitude);
        builder.Ignore(b => b.Longitude);
        
        builder.Property(b => b.Municipality).IsRequired().HasMaxLength(100);
        builder.Property(b => b.PostalCode).IsRequired().HasMaxLength(8);
        builder.Property(b => b.City).IsRequired().HasMaxLength(100);
        builder.Property(b => b.IsSmallCell).IsRequired();
        builder.HasOne(b => b.Provider)
            .WithMany(p => p.BaseStations)
            .HasForeignKey(b => b.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}