using Serilog;
using Serilog.Events;

namespace AntennaScraper.Api.Helper;

public static class LogHelpers
{
    private const string LogTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] ({SourceContext}) ({RequestId}) {Message:lj}{NewLine}{Exception}";

    public static LoggerConfiguration Development()
    {
        var loggerConfiguration = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithClientIp()
            .MinimumLevel.Information()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day, outputTemplate: LogTemplate)
            .WriteTo.Logger(lc => lc
                .DefaultFilters()
                .WriteTo.Console(outputTemplate: LogTemplate)
            );

        return loggerConfiguration;
    }

    public static LoggerConfiguration Production()
    {
        var loggerConfiguration = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithClientIp()
            .MinimumLevel.Information()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day, outputTemplate: LogTemplate)
            .WriteTo.Logger(lc => lc
                .DefaultFilters()
                .WriteTo.Console(outputTemplate: LogTemplate)
            );

        return loggerConfiguration;
    }

    private static Func<LogEvent, bool> MinimumLevel(string sourceContext, LogEventLevel level)
    {
        return logEvent => logEvent.Level < level &&
                           logEvent.Properties.GetValueOrDefault("SourceContext")?.ToString().Trim('"').StartsWith(sourceContext) == true;
    }

    private static LoggerConfiguration DefaultFilters(this LoggerConfiguration loggerConfiguration)
    {
        return loggerConfiguration
            .Filter.ByExcluding(MinimumLevel("Microsoft.EntityFrameworkCore", LogEventLevel.Warning))
            .Filter.ByExcluding(MinimumLevel("System.Net.Http.HttpClient", LogEventLevel.Warning));
    }
}