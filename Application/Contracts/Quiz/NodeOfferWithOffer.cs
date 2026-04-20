using Domain.Model.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Quiz
{
    public sealed record NodeOfferWithOffer(NodeOffer Link, Offer Offer);
}
