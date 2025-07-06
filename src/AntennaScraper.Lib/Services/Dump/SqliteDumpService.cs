using AntennaScraper.Lib.Persistence;
using AntennaScraper.Lib.Services.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace AntennaScraper.Lib.Services.Dump;

public class SqliteDumpService(IUnitOfWork uow) : IDumpService
{
    public async Task<Stream> DumpDbAsync(CancellationToken cancellationToken)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var options = new DbContextOptionsBuilder<AntennaDbContext>()
                .UseSqlite($"Data Source={tempFile};Pooling=false")
                .Options;
            await using (var sqliteContext = new AntennaDbContext(options))
            {
                await sqliteContext.Database.EnsureCreatedAsync(cancellationToken);

                await uow.ExecuteTransactionAsync(async (ct, ctx) =>
                {
                    var rowVersion = 0u;
                    var providers = await ctx.Providers.AsNoTracking().ToListAsync(ct);
                    foreach (var provider in providers)
                    {
                        provider.RowVersion = rowVersion++;
                    }

                    var bands = await ctx.Bands.AsNoTracking().ToListAsync(ct);
                    foreach (var band in bands)
                    {
                        band.RowVersion = rowVersion++;
                    }

                    var carriers = await ctx.Carriers.AsNoTracking().ToListAsync(ct);
                    foreach (var carrier in carriers)
                    {
                        carrier.RowVersion = rowVersion++;
                    }

                    var baseStations = await ctx.BaseStations.AsNoTracking().ToListAsync(ct);
                    foreach (var baseStation in baseStations)
                    {
                        baseStation.Longitude = baseStation.Location.X;
                        baseStation.Latitude = baseStation.Location.Y;
                        baseStation.RowVersion = rowVersion++;
                    }

                    var antennas = await ctx.Antennas.AsNoTracking().ToListAsync(ct);
                    foreach (var antenna in antennas)
                    {
                        antenna.RowVersion = rowVersion++;
                    }

                    await sqliteContext.Providers.AddRangeAsync(providers, ct);
                    await sqliteContext.Bands.AddRangeAsync(bands, ct);
                    await sqliteContext.Carriers.AddRangeAsync(carriers, ct);
                    await sqliteContext.BaseStations.AddRangeAsync(baseStations, ct);
                    await sqliteContext.Antennas.AddRangeAsync(antennas, ct);
                    await sqliteContext.SaveChangesAsync(ct);
                }, cancellationToken);
            }

            var ms = new MemoryStream();
            await using (var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await fileStream.CopyToAsync(ms, cancellationToken);
            }

            ms.Position = 0;
            return ms;
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}