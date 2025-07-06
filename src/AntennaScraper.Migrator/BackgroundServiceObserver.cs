using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AntennaScraper.Migrator;

public class BackgroundServiceObserver(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        var backgroundServiceTasks = serviceProvider.GetServices<IHostedService>()
            .OfType<BackgroundService>().Select(s => s.ExecuteTask);

        if (backgroundServiceTasks.Any(t => t?.IsFaulted == true)) Environment.ExitCode = -1;

        return Task.CompletedTask;
    }
}