using CRM.Chat.Domain.Common.Entities;

namespace CRM.Chat.Application.Common.Abstractions.Repositories;

public interface IRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    IQueryable<TEntity> GetQueryable();
}