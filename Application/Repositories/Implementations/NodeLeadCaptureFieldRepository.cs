using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Implementations
{
    public class NodeLeadCaptureFieldRepository(AppDbContext context)
    : GenericRepository<NodeLeadCaptureField>(context), INodeLeadCaptureFieldRepository
    {

    }
}
