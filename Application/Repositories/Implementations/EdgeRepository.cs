using Application.Repositories.Interfaces;
using Domain.Model.AdminProfile;
using Domain.Model.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Implementations
{
    public class EdgeRepository(AppDbContext context) : GenericRepository<Edge>(context), IEdgeRepository
    {
    }
}
