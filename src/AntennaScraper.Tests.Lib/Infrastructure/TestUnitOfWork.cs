using AntennaScraper.Lib.Services.UnitOfWork;

namespace AntennaScraper.Tests.Lib.Infrastructure;

public class TestUnitOfWork(AntennaDbContext context) : IUnitOfWork
{
    public async Task<T> ExecuteTransactionAsync<T>(Func<CancellationToken, AntennaDbContext, Task<T>> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action(cancellationToken, context);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExecuteTransactionAsync(Func<CancellationToken, AntennaDbContext, Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteTransactionAsync<object?>(async (ct, ctx) =>
        {
            await action(ct, ctx);
            return null;
        }, cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, AntennaDbContext, Task<T>> action, CancellationToken cancellationToken = default)
    {
        return await action(cancellationToken, context);
    }

    public async Task ExecuteAsync(Func<CancellationToken, AntennaDbContext, Task> action, CancellationToken cancellationToken = default)
    {
        await action(cancellationToken, context);
    }
}