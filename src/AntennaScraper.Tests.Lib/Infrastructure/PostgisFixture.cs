using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace AntennaScraper.Tests.Lib.Infrastructure;

public sealed class PostgisFixture : IAsyncLifetime
{
    private IContainer _db = null!;

    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _db = new ContainerBuilder("postgis/postgis:17-3.5-alpine")
            .WithEnvironment("POSTGRES_DB", "antenna_test")
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .WithPortBinding(5432, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
            .Build();

        await _db.StartAsync();

        var hostPort = _db.GetMappedPublicPort(5432);
        ConnectionString = $"Host=localhost;Port={hostPort};Database=antenna_test;Username=postgres;Password=postgres";
    }

    public Task DisposeAsync()
    {
        return _db.DisposeAsync().AsTask();
    }

    public AntennaDbContext CreateContext()
    {
        var opts = new DbContextOptionsBuilder<AntennaDbContext>()
            .UseNpgsql(ConnectionString, o => o.UseNetTopologySuite())
            .Options;
        return new AntennaDbContext(opts);
    }
}