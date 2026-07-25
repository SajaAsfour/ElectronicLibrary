using ElectronicLibrary.DAL.Repositories.Generic;

namespace ElectronicLibrary.DAL.Repositories.UnitOfWork;

public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}