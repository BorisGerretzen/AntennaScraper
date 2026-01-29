namespace AntennaScraper.Tests.Lib.Infrastructure;

public sealed class SqliteFixture : IDisposable
{
    private readonly List<string> _dbPaths = [];
    private readonly List<AntennaDbContext> _contexts = [];

    /// <summary>
    /// Creates a new SQLite database context with a unique temporary file.
    /// The database schema is automatically created.
    /// </summary>
    public AntennaDbContext CreateContext()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
        _dbPaths.Add(dbPath);

        var options = new DbContextOptionsBuilder<AntennaDbContext>()
            .UseSqlite($"DataSource={dbPath}")
            .Options;

        var context = new AntennaDbContext(options);
        context.Database.EnsureCreated();
        _contexts.Add(context);

        return context;
    }

    /// <summary>
    /// Creates a TestUnitOfWork wrapper around the given context.
    /// </summary>
    public static TestUnitOfWork CreateUnitOfWork(AntennaDbContext context)
    {
        return new TestUnitOfWork(context);
    }

    public void Dispose()
    {
        foreach (var context in _contexts)
        {
            context.Database.EnsureDeleted();
            context.Dispose();
        }

        foreach (var dbPath in _dbPaths)
            if (File.Exists(dbPath))
                File.Delete(dbPath);

        _contexts.Clear();
        _dbPaths.Clear();
    }
}