using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.Repositories.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLibrary.DAL.Repositories.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;

    public UnitOfWork(ApplicationDbContext context,IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public IGenericRepository<T> Repository<T>() where T : class
    {
        return _serviceProvider.GetRequiredService<IGenericRepository<T>>();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}