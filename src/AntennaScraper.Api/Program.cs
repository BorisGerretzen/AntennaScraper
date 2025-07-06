using AntennaScraper.Api.Endpoints;
using AntennaScraper.Api.Helper;
using AntennaScraper.Api.Services;
using AntennaScraper.Lib;
using NetTopologySuite.IO.Converters;
using Serilog;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);

// Logging
var loggerConfiguration = builder.Environment.IsDevelopment()
    ? LogHelpers.Development()
    : LogHelpers.Production();

try
{
    builder.AddAntennaScraperLib();
    builder.Services.AddOpenApi();
    builder.Services.AddSerilog(loggerConfiguration.CreateLogger());

    builder.Services.AddHostedService<BackgroundSyncService>();

    builder.Services.Configure<JsonOptions>(options => { options.SerializerOptions.Converters.Add(new GeoJsonConverterFactory()); });

    var app = builder.Build();
    app.UseSerilogRequestLogging();
    app.MapOpenApi("/openapi.json");

    app.Map<IndexEndpoint>();
    app.Map<SyncLogEndpoint>();
    app.Map<GetAllEndpoint>();
    app.Map<GetStatsEndpoint>();
    app.Map<DumpEndpoint>();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}