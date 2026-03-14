using Application.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Implementations
{
    public class EFUnitOfWork(AppDbContext context) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync()
            => context.SaveChangesAsync();

        public Task<int> SaveChangesAsync(CancellationToken ct)
            => context.SaveChangesAsync(ct);
    }
}
