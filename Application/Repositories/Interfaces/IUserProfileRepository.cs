using Application.Contracts.Auth;
using Domain.Model.AdminProfile;

namespace Application.Repositories.Interfaces
{
    public interface IUserProfileRepository : IGenericRepository<UserProfile>
    {
        Task<MeResponse?> GetMeAsync(Guid applicationUserId, CancellationToken ct = default);
    }
}
