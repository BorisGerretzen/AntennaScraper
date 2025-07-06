using AntennaScraper.Lib.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntennaScraper.Lib.Persistence.EntityTypeConfigurations;

public class AntennaEntityTypeConfiguration : IEntityTypeConfiguration<Antenna>
{
    public void Configure(EntityTypeBuilder<Antenna> builder)
    {
        builder.Property(a => a.Frequency).IsRequired();
        builder.Property(a => a.Height).IsRequired();
        builder.Property(a => a.Direction).IsRequired();
        builder.Property(a => a.TransmissionPower).IsRequired();

        builder.Property(a => a.SatCode)
            .IsRequired()
            .HasMaxLength(10);
        builder.Property(a => a.IsDirectional).IsRequired();

        builder.HasOne(a => a.BaseStation)
            .WithMany(bs => bs.Antennas)
            .HasForeignKey(a => a.BaseStationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Carrier)
            .WithMany()
            .HasForeignKey(a => a.CarrierId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}