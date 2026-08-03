using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.Repositories.Generic;
using ElectronicLibrary.DAL.Repositories.UnitOfWork;

namespace ElectronicLibrary.UnitTests.Helpers;

public sealed class TestUnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public TestUnitOfWork(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<T> Repository<T>()
        where T : class
    {
        return new GenericRepository<T>(_context);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(
            cancellationToken);
    }
}