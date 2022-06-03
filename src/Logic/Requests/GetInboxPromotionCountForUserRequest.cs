using MediatR;

namespace CloutCast.Requests
{
    using Contracts;

    public class GetInboxPromotionCountForUserRequest : IRequest<long>
    {
        private ActiveFlag _flag = ActiveFlag.Both;
        private IBitCloutUser _activeUser;

        public GetInboxPromotionCountForUserRequest Active(ActiveFlag? flag) => this.Fluent(x => _flag = flag ?? ActiveFlag.Both);
        public ActiveFlag Active() => _flag;

        public GetInboxPromotionCountForUserRequest ActiveUser(IBitCloutUser user) 
            => this.Fluent(x => _activeUser = user);

        public IBitCloutUser ActiveUser() => _activeUser;
    }
}