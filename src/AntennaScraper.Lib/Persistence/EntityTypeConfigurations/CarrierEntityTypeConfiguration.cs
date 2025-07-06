using AntennaScraper.Lib.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntennaScraper.Lib.Persistence.EntityTypeConfigurations;

public class CarrierEntityTypeConfiguration : IEntityTypeConfiguration<Carrier>
{
    public void Configure(EntityTypeBuilder<Carrier> builder)
    {
        builder.Property(pb => pb.FrequencyLow).IsRequired();
        builder.Property(pb => pb.FrequencyHigh).IsRequired();

        builder.HasOne(pb => pb.Provider)
            .WithMany(p => p.Carriers)
            .HasForeignKey(pb => pb.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pb => pb.Band)
            .WithMany()
            .HasForeignKey(pb => pb.BandId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}