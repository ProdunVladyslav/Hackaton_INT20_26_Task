using Application.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;


namespace Application.Repositories.Implementations
{
    public class EFUnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _currentTransaction;

        public EFUnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct)
        {
            return await _context.SaveChangesAsync(ct);
        }

        // ── Explicit transaction — use when you need multiple save points ────────
        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTransaction != null)
                throw new InvalidOperationException("A transaction is already in progress.");

            _currentTransaction = await _context.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            if (_currentTransaction == null)
                throw new InvalidOperationException("No active transaction to commit.");

            try
            {
                await _context.SaveChangesAsync(ct);
                await _currentTransaction.CommitAsync(ct);
            }
            catch
            {
                await RollbackAsync(ct);
                throw;  // re-throw so the caller knows it failed
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        public async Task RollbackAsync(CancellationToken ct = default)
        {
            if (_currentTransaction == null) return;

            await _currentTransaction.RollbackAsync(ct);
            await DisposeTransactionAsync();
        }

        private async Task DisposeTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }
}
