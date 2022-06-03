using System.Collections.Generic;
using JetBrains.Annotations;
using MediatR;

namespace CloutCast.Requests
{
    using Contracts;
    using Entities;

    [UsedImplicitly]
    public class GetInboxPromotionsForUserRequest : IRequest<List<Promotion>>
    {
        private ActiveFlag _flag = ActiveFlag.Both;
        private IBitCloutUser _activeUser;

        public GetInboxPromotionsForUserRequest Active(ActiveFlag? flag) => this.Fluent(x => _flag = flag ?? ActiveFlag.Both);
        public ActiveFlag Active() => _flag;

        public GetInboxPromotionsForUserRequest ActiveUser(IBitCloutUser user) => this.Fluent(x => _activeUser = user);
        public IBitCloutUser ActiveUser() => _activeUser;
    }
}