using AntennaScraper.Lib.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AntennaScraper.Lib.Services.UnitOfWork;

public class DefaultUnitOfWork(IDbContextFactory<AntennaDbContext> contextFactory) : IUnitOfWork
{
    public async Task<T> ExecuteTransactionAsync<T>(Func<CancellationToken, AntennaDbContext, Task<T>> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
            var executionStrategy = dbContext.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var result = await action(cancellationToken, dbContext);
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while executing a database action.", ex);
        }
    }

    public async Task ExecuteTransactionAsync(Func<CancellationToken, AntennaDbContext, Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteTransactionAsync<Task>(async (ct, context) =>
        {
            await action(ct, context);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, AntennaDbContext, Task<T>> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
            return await action(cancellationToken, dbContext);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while executing a database action.", ex);
        }
    }

    public async Task ExecuteAsync(Func<CancellationToken, AntennaDbContext, Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync<Task>(async (ct, context) =>
        {
            await action(ct, context);
            return Task.CompletedTask;
        }, cancellationToken);
    }
}