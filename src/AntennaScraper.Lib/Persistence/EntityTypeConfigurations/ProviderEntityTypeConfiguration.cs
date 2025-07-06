using AntennaScraper.Lib.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntennaScraper.Lib.Persistence.EntityTypeConfigurations;

public class ProviderEntityTypeConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
    }
}