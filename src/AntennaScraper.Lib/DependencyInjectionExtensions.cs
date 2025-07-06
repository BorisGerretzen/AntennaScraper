using AntennaScraper.Lib.Persistence;
using AntennaScraper.Lib.Services.Data.BandService;
using AntennaScraper.Lib.Services.Data.CarrierService;
using AntennaScraper.Lib.Services.Data.ProviderService;
using AntennaScraper.Lib.Services.Dump;
using AntennaScraper.Lib.Services.External.AntenneKaartClient;
using AntennaScraper.Lib.Services.External.AntenneRegisterClient;
using AntennaScraper.Lib.Services.Stats;
using AntennaScraper.Lib.Services.Sync.BandSyncService;
using AntennaScraper.Lib.Services.Sync.BaseStationSyncService;
using AntennaScraper.Lib.Services.Sync.BaseSyncService;
using AntennaScraper.Lib.Services.Sync.CarrierSyncService;
using AntennaScraper.Lib.Services.Sync.ProviderSyncService;
using AntennaScraper.Lib.Services.SyncLogService;
using AntennaScraper.Lib.Services.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AntennaScraper.Lib;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddAntennaScraperLib(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(AntennaGlobals.DbConnectionStringName);
        if (string.IsNullOrEmpty(connectionString)) throw new InvalidOperationException("Database connection string is not configured.");

        var services = builder.Services;
        services.AddPooledDbContextFactory<AntennaDbContext>(options =>
        {
            if (builder.Environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }

            options.UseNpgsql(builder.Configuration.GetConnectionString(AntennaGlobals.DbConnectionStringName), sqlOptions => { sqlOptions.UseNetTopologySuite(); });
        });

        services.AddHttpClient<IAntenneKaartClient, AntenneKaartClient>(c =>
        {
            c.BaseAddress = new Uri("https://antennekaart.nl/api/v1/");
            c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "AntennaScraper/1.0 (contact: antenna@gerretzen.eu)");
            c.DefaultRequestHeaders.Add("X-Purpose", "Open data scraping for research purposes at University of Twente");
        });
        
        services.AddHttpClient<IAntenneRegisterClient, DefaultAntenneRegisterClient>(c =>
        {
            c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "AntennaScraper/1.0 (contact: antenna@gerretzen.eu)");
            c.DefaultRequestHeaders.Add("X-Purpose", "Open data scraping for research purposes at University of Twente");
        });
        // services.AddScoped<IAntenneRegisterClient, FileMockAntenneRegisterClient>();
        
        services
            .AddScoped<IProviderService, AntenneKaartProviderService>()
            .AddScoped<ICarrierService, StaticCarrierService>()
            .AddScoped<IBandService, StaticBandService>();

        services
            .AddScoped<IBaseSyncService, BaseSyncService>()
            .AddScoped<ICarrierSyncService, CarrierSyncService>()
            .AddScoped<IBandSyncService, BandSyncService>()
            .AddScoped<IProviderSyncService, ProviderSyncService>()
            .AddScoped<IBaseStationSyncService, BaseStationSyncService>();

        services
            .AddScoped<ISyncLogService, DefaultSyncLogService>()
            .AddScoped<IStatsService, DefaultStatsService>()
            .AddScoped<IDumpService, SqliteDumpService>();
        
        services
            .AddScoped<IUnitOfWork, DefaultUnitOfWork>();

        return services;
    }
}