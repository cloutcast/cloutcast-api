using System.Collections.Generic;
using JetBrains.Annotations;
using MediatR;

namespace CloutCast.Requests
{
    using Contracts;
    using Entities;

    [UsedImplicitly]
    public class GetPromotionListRequest : IRequest<List<Promotion>>
    {
        private ActiveFlag _flag = ActiveFlag.Both;
        private IBitCloutUser _activeUser;
        private long? _coinPrice;
        private long ?_followerCount;

        public GetPromotionListRequest Active(ActiveFlag? flag) => this.Fluent(x => _flag = flag ?? ActiveFlag.Both);
        public ActiveFlag Active() => _flag;

        public GetPromotionListRequest ActiveUser(IBitCloutUser user) => this.Fluent(x => _activeUser = user);
        public IBitCloutUser ActiveUser() => _activeUser;

        public GetPromotionListRequest CoinPrice(long? coinPrice) => this.Fluent(x => _coinPrice = coinPrice);
        public long CoinPrice() => _coinPrice ?? 0;

        public GetPromotionListRequest FollowerCount(long? followerCount) => this.Fluent(x => _followerCount = followerCount);
        public long FollowerCount() => _followerCount ?? 0;
    }
}