using System.Collections.Generic;
using JetBrains.Annotations;
using MediatR;

namespace CloutCast.Requests
{
    using Entities;

    [UsedImplicitly]
    public class GetPromotionHeaderListRequest : IRequest<List<Promotion>>
    {
        private ActiveFlag _flag = ActiveFlag.Both;

        public GetPromotionHeaderListRequest Active(ActiveFlag? flag) => this.Fluent(x => _flag = flag ?? ActiveFlag.Both);
        public ActiveFlag Active() => _flag;
    }
}