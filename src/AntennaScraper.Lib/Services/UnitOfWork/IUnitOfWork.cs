using AntennaScraper.Lib.Persistence;

namespace AntennaScraper.Lib.Services.UnitOfWork;

public interface IUnitOfWork
{
    Task<T> ExecuteTransactionAsync<T>(Func<CancellationToken, AntennaDbContext, Task<T>> action, CancellationToken cancellationToken = default);
    Task ExecuteTransactionAsync(Func<CancellationToken, AntennaDbContext, Task> action, CancellationToken cancellationToken = default);
    Task<T> ExecuteAsync<T>(Func<CancellationToken, AntennaDbContext, Task<T>> action, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<CancellationToken, AntennaDbContext, Task> action, CancellationToken cancellationToken = default);
}