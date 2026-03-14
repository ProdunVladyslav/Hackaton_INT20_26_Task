using Application.Repositories.Interfaces;
using Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Implementations
{
    public class UserProfileRepository(AppDbContext context) : GenericRepository<UserProfile>(context), IUserProfileRepository
    { 

    }
}
