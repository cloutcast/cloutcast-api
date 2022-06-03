using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using log4net;

namespace CloutCast.Handlers
{
    using Contracts;
    using Requests;
    using Models;

    [JetBrains.Annotations.UsedImplicitly]
    public class GetPostByHashHexHandler : ValidatedHandler<GetPostByHashHexHandler, GetPostByHashHexRequest, IBitCloutPost>
    {
        protected class ResultRoot
        {
            public string Error { get; set; }
            public Post PostFound { get; set; }
        }

        private readonly IBitCloutRestFactory _factory;
        private readonly ILog _logger;
        private Post _result;

        public GetPostByHashHexHandler(IBitCloutRestFactory factory, ILog logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public override async Task<IBitCloutPost> Handle(GetPostByHashHexRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var client = _factory.CreateClient(BitCloutEndPoints.SinglePost);
            var policy = _factory.CreatePolicy<ResultRoot>(1);
            var root = _factory.Execute(client, policy, request.ToBody());
            _result = ParseResult(root);

            if (request.ThrowOnMissingPost)
                await ValidateAndThrowAsync(cancellationToken);

            return _result;
        }

        protected Post ParseResult(ResultRoot root)
        {
            var err = (root?.Error ?? "").Trim();
            if (err.IsNotEmpty())
            {
                _logger.Error(err);
                if (err.Contains("The poster public key for this post is restricted"))
                    return new Post {IsHidden = true};
            }

            else if (root?.PostFound != null)
                return root.PostFound;

            return null;
        }

        protected override void SetupValidation(HandlerValidator validator) => validator.RuleFor(r => r._result).NotNull();
    }
}