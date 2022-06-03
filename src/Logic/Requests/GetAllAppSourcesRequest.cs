using System.Collections.Generic;
using MediatR;

namespace CloutCast.Requests
{
    using Entities;

    public class GetAllAppSourcesRequest : IRequest<List<AppSource>> { }
}