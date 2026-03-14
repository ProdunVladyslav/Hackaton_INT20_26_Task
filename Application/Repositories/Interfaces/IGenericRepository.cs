using System.Linq.Expressions;

namespace Application.Repositories.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        // Read
        Task<T?> GetByIdAsync(object id, CancellationToken ct = default);
        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

        // Flexible querying
        Task<IReadOnlyList<T>> FindAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken ct = default);

        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken ct = default);

        Task<bool> AnyAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken ct = default);

        // Create
        Task AddAsync(T entity, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

        // Update
        void Update(T entity);

        // Delete
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);

        // Expose queryable
        IQueryable<T> Query();
    }
}
