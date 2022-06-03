using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace CloutCast.Handlers
{
    using Contracts;
    using Models;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class GetPostsForUserHandler : IRequestHandler<GetPostsForUserRequest, List<IBitCloutPost>>
    {
        protected class GetPostsJsonBodyRequest
        {
            public string PublicKeyBase58Check = "";
            public string Username = "";
            public string ReaderPublicKeyBase58Check = "";
            public string LastPostHashHex = "";
            public int NumToFetch = 100;
        }
        protected class Root
        {
            public List<Post> Posts { get; set; }
            public string LastPostHashHex { get; set; }
        }

        private readonly IBitCloutRestFactory _factory;
        public GetPostsForUserHandler(IBitCloutRestFactory factory) => _factory = factory;

        public async Task<List<IBitCloutPost>> Handle(GetPostsForUserRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var body = MakeBody(request.TargetUser, request.FetchDepth, request.StartFromPostHex);

            var client = _factory.CreateClient(BitCloutEndPoints.UserPosts);
            var policy = _factory.CreatePolicy<Root>(2);
            var root = _factory.Execute(client, policy, body);

            return root?.Posts.Cast<IBitCloutPost>().ToList() ?? new List<IBitCloutPost>();
        }

        protected static GetPostsJsonBodyRequest MakeBody(IBitCloutUser targetUser, int fetchCount, string postHex = "") =>
            new GetPostsJsonBodyRequest
            {
                PublicKeyBase58Check = targetUser.PublicKey,
                NumToFetch = fetchCount,
                LastPostHashHex = postHex
            };

    }
}