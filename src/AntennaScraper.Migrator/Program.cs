using AntennaScraper.Lib;
using AntennaScraper.Lib.Persistence;
using AntennaScraper.Migrator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<DbInitializer>();
builder.Services.AddHostedService<BackgroundServiceObserver>();

builder.Services.AddDbContextPool<AntennaDbContext>(options =>
{
    options.EnableSensitiveDataLogging();
    options.UseNpgsql(builder.Configuration.GetConnectionString(AntennaGlobals.DbConnectionStringName), sqlOptions =>
    {
        sqlOptions.UseNetTopologySuite();
        sqlOptions.MigrationsAssembly(typeof(AntennaDbContext).Assembly.FullName);
    });
});

var app = builder.Build();

app.Run();