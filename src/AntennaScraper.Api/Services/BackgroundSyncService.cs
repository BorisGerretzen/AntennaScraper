using System.Text.Json;
using AntennaScraper.Lib;
using AntennaScraper.Lib.Services.Data.BandService;
using AntennaScraper.Lib.Services.Data.CarrierService;
using AntennaScraper.Lib.Services.Data.ProviderService;
using AntennaScraper.Lib.Services.External.AntenneRegisterClient;
using AntennaScraper.Lib.Services.External.AntenneRegisterClient.Dto;
using AntennaScraper.Lib.Services.Sync.BandSyncService;
using AntennaScraper.Lib.Services.Sync.BaseStationSyncService;
using AntennaScraper.Lib.Services.Sync.CarrierSyncService;
using AntennaScraper.Lib.Services.Sync.ProviderSyncService;
using AntennaScraper.Lib.Services.SyncLogService;
using AntennaScraper.Lib.Services.UnitOfWork;

namespace AntennaScraper.Api.Services;

public class BackgroundSyncService(IServiceScopeFactory scopeFactory, ILogger<BackgroundSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Background sync service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Starting background sync cycle...");
            
            using var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var syncLogService = sp.GetRequiredService<ISyncLogService>();
            var startTime = DateTime.UtcNow;

            try
            {
                await SyncBackgroundData(sp, stoppingToken);
                await SyncAntennas(sp, stoppingToken);

                logger.LogInformation("Sync completed successfully a {CompletedAt}, in {Seconds}s.", DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"),
                    (DateTime.UtcNow - startTime).TotalSeconds.ToString("F1"));
                await syncLogService.LogSyncSuccessAsync(startTime, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during background sync.");
                await syncLogService.LogSyncFailureAsync(startTime, stoppingToken);
            }
            finally
            {
                var nextRun = DateTime.UtcNow.AddHours(24);
                logger.LogInformation("Next sync scheduled at {NextRun}.", nextRun.ToString("dd MMM yyyy HH:mm"));
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        logger.LogInformation("Background sync service stopped.");
    }

    private async Task SyncBackgroundData(IServiceProvider sp, CancellationToken stoppingToken)
    {
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var providerService = sp.GetRequiredService<IProviderService>();
        var bandService = sp.GetRequiredService<IBandService>();
        var carrierService = sp.GetRequiredService<ICarrierService>();

        var providerSync = sp.GetRequiredService<IProviderSyncService>();
        var bandSync = sp.GetRequiredService<IBandSyncService>();
        var carrierSync = sp.GetRequiredService<ICarrierSyncService>();

        logger.LogInformation("Fetching data for providers, bands, and carriers...");
        var startFetch = DateTime.UtcNow;
        var providers = await providerService.GetProvidersAsync(stoppingToken);
        var bands = await bandService.GetBandsAsync(stoppingToken);
        var carriers = await carrierService.GetCarriersAsync(stoppingToken);
        logger.LogInformation("Fetching complete in {Seconds}s", (DateTime.UtcNow - startFetch).TotalSeconds.ToString("F1"));

        logger.LogInformation("Starting sync for {ProviderCount} providers, {BandCount} bands, and {CarrierCount} carriers.",
            providers.Count, bands.Count, carriers.Count);
        var startTimeSync = DateTime.UtcNow;
        await uow.ExecuteTransactionAsync(async (token, _) =>
        {
            await providerSync.SyncProvidersAsync(providers, token);
            await bandSync.SyncBandsAsync(bands, token);
            await carrierSync.SyncCarriersAsync(carriers, token);
        }, stoppingToken);
        logger.LogInformation("Static sync completed in {Seconds}s.", (DateTime.UtcNow - startTimeSync).TotalSeconds.ToString("F1"));
    }

    private async Task SyncAntennas(IServiceProvider sp, CancellationToken stoppingToken, bool writeToFile = false, bool readFromFile = false)
    {
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var baseStationSync = sp.GetRequiredService<IBaseStationSyncService>();
        var antenneRegisterClient = sp.GetRequiredService<IAntenneRegisterClient>();

        logger.LogInformation("Fetching base stations and antennas from Antenne Register...");
        var startFetch = DateTime.UtcNow;
        
        List<AntenneRegisterBaseStation> incomingBs;
        if (!readFromFile)
        {
            incomingBs = await antenneRegisterClient.GetBaseStationsAsync(stoppingToken);
        }
        else
        {
            await using var bsStream = new FileStream("bs.json", FileMode.Open, FileAccess.Read);
            incomingBs = await JsonSerializer.DeserializeAsync<List<AntenneRegisterBaseStation>>(bsStream, AntennaGlobals.GeoSerializer, stoppingToken)
                             ?? [];
        }

        incomingBs = incomingBs.DistinctBy(bs => bs.Id).ToList();
        logger.LogInformation("Fetched {BaseStationCount} base stations.", incomingBs.Count);
        
        var antennaRequest = incomingBs.ToDictionary(bs => bs.Id, bs => bs.AntennaIds);
        Dictionary<long, List<AntenneRegisterAntenna>> incomingAntennas;
        if (!readFromFile)
        {
            incomingAntennas = await antenneRegisterClient.GetAntennasByBaseStationIdAsync(antennaRequest, stoppingToken);
        }
        else
        {
            await using var antennaStream = new FileStream("antennas.json", FileMode.Open, FileAccess.Read);
            incomingAntennas = await JsonSerializer.DeserializeAsync<Dictionary<long, List<AntenneRegisterAntenna>>>(antennaStream, AntennaGlobals.GeoSerializer, stoppingToken)
            ?? new Dictionary<long, List<AntenneRegisterAntenna>>();
        }

        logger.LogInformation("Fetching complete in {Seconds}s", (DateTime.UtcNow - startFetch).TotalSeconds.ToString("F1"));

        if (writeToFile)
        {
            logger.LogInformation("Writing base stations and antennas to JSON files.");
            await using var fileStream = new FileStream("bs.json", FileMode.Create, FileAccess.Write);
            await JsonSerializer.SerializeAsync(fileStream, incomingBs, AntennaGlobals.GeoSerializer, stoppingToken);
            await using var antennaStream = new FileStream("antennas.json", FileMode.Create, FileAccess.Write);
            await JsonSerializer.SerializeAsync(antennaStream, incomingAntennas, AntennaGlobals.GeoSerializer, stoppingToken);
            logger.LogInformation("JSON files written successfully for layer.");
        }

        logger.LogInformation("Starting base station sync for {BaseStationCount} base stations and {AntennaCount} antennas.",
            incomingBs.Count,
            incomingAntennas.SelectMany(bs => bs.Value).Count());
        var startTimeSync = DateTime.UtcNow;
        await uow.ExecuteTransactionAsync(async (token, _) => { await baseStationSync.SyncBaseStationsAsync(incomingBs, incomingAntennas, token); }, stoppingToken);
        logger.LogInformation("Sync completed in {Seconds}s.", (DateTime.UtcNow - startTimeSync).TotalSeconds.ToString("F1"));
    }
}