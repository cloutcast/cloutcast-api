using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CloutCast.Handlers
{
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class GetFollowerCountForUserHandler : IRequestHandler<GetFollowerCountForUserRequest, long>
    {
        protected class Root { public int NumFollowers { get; set; } }

        private readonly IBitCloutRestFactory _factory;
        public GetFollowerCountForUserHandler(IBitCloutRestFactory factory) => _factory = factory;

        public async Task<long> Handle(GetFollowerCountForUserRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var client = _factory.CreateClient(BitCloutEndPoints.UserFollowerCount);
            var policy = _factory.CreatePolicy<Root>(5);
            var root = _factory.Execute(client, policy, request.ToBody());

            return root?.NumFollowers ?? 0;
        }
    }
}