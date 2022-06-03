using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Entities;
    using Models;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class GetUserHandler : ValidatedHandler<GetUserHandler, GetUserRequest, BitCloutUser>
    {
        private readonly IMediator _mediator;
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        private BitCloutUser _user;

        public GetUserHandler(IMediator mediator, IDapperPipeline pipeline, ILog log)
        {
            _mediator = mediator;
            _pipeline = pipeline;
            _log = log;
        }

        public override async Task<BitCloutUser> Handle(GetUserRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var includeProfile = request.IncludeProfile();
            var emptyUser = new BitCloutUser
            {
                Profile = includeProfile ? new UserProfile() : null,
                PublicKey = request.PublicKey()
            };

            _user = GetById(request.UserId(), includeProfile) ??
                    GetByPublicKey(emptyUser.PublicKey, includeProfile);

            if (request.ThrowOnNotFound())
                await ValidateAndThrowAsync(cancellationToken);

            if (!request.FetchFromBitClout()) return _user;

            return await UpdateUserFromBitClout(_user ?? emptyUser, request.IncludeFollowerCount());
        }

        protected override void SetupValidation(HandlerValidator v) => v.RuleFor(r => r._user).NotNull();
        
        protected BitCloutUser GetById(long userId, bool includeProfile)
        {
            if (userId <= 0) return null;
            BitCloutUser user = null;
            _pipeline
                .Query<IGetBitCloutUserQuery, BitCloutUser>(
                    q => q.UserId(userId).IncludeProfile(includeProfile),
                    u => user = u)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();
            return user;
        }
        protected BitCloutUser GetByPublicKey(string publicKey, bool includeProfile)
        {
            BitCloutUser user = null;
            _pipeline
                .Query<GetBitCloutUserQuery, BitCloutUser>(
                    q => q.PublicKey(publicKey).IncludeProfile(includeProfile),
                    u => user = u)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();
            return user;
        }

        protected async Task<BitCloutUser> UpdateUserFromBitClout(BitCloutUser source, bool includeFollowerCount)
        {
            if (source == null || source.PublicKey.IsEmpty()) return source;

            _log.Info("Fetching User data from BitClout");
            var singleProfile = await _mediator.Send<GetCoinPriceForUserRequest, SingleUserProfile>(r => r.User = source);
            source.Handle = singleProfile.Profile.Username;

            source.CoinPrice = singleProfile.Profile?.CoinPriceBitCloutNanos ?? 0;
            source.BlackList = source.BlackList || singleProfile.IsBlacklisted;
            if (includeFollowerCount)
                source.FollowerCount = await _mediator.Send<GetFollowerCountForUserRequest, long>(r => r.User = source);

            return source;
        }
    }
}