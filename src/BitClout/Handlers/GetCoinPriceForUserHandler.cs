using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CloutCast.Handlers
{
    using Models;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class GetCoinPriceForUserHandler : IRequestHandler<GetCoinPriceForUserRequest, SingleUserProfile>
    {
        private readonly IBitCloutRestFactory _factory;
        public GetCoinPriceForUserHandler(IBitCloutRestFactory factory) => _factory = factory;

        public async Task<SingleUserProfile> Handle(GetCoinPriceForUserRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var client = _factory.CreateClient(BitCloutEndPoints.UserCoinPrice);
            var policy = _factory.CreatePolicy<SingleUserProfile>(5);
            var root = _factory.Execute(client, policy, new
            {
                PublicKeyBase58Check = request.User.PublicKey
            });

            if (root == null) throw new CloutCastException("User Coin Price not found", HttpStatusCode.NotFound);

            return root;
        }
    }
}