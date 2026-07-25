using System.Linq.Expressions;

namespace ElectronicLibrary.DAL.Repositories.Generic;

public interface IGenericRepository<T> where T : class
{
    IQueryable<T> Query();
    Task<T?> GetOneAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task AddAsync(T entity,
        CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
}