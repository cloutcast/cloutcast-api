using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Contracts;
    using Entities;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class FindProofOfWorkHandler : ValidatedHandler<FindProofOfWorkHandler, FindProofOfWorkRequest, IBitCloutPost>
    {
        private readonly IMediator _mediator;
        private readonly ILog _log;

        private PromotionActivity _action;
        private IBitCloutPost _match;

        public FindProofOfWorkHandler(IMediator mediator, ILog log)
        {
            _mediator = mediator;
            _log = log;
        }

        public override async Task<IBitCloutPost> Handle(FindProofOfWorkRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken, "initialRequest");

            var promotion = request.Promotion;
            var promoter = request.Promoter;
            var promoStart = promotion.Events.Started().TimeStamp;
            var target = promotion.Target;
            _action = target.Action;

            if (_action == PromotionActivity.Comment)
                _match = await FindComment(promoter, target, promoStart);
            else
                _match = await FindTargetPost(promoter, target, _action, promoStart);

            await ValidateAndThrowAsync(cancellationToken);

            return _match;
        }

        private static bool IsPostAReClout(IBitCloutPost post) => post.Body.IsEmpty();
        private static bool IsPostAQuote(IBitCloutPost post) => post.Body.IsNotEmpty();

        protected internal async Task<IBitCloutPost> FindComment(IBitCloutUser promoter, IPromotionTarget target, DateTimeOffset promoStart)
        {
            var post = await _mediator.Send<GetPostByHashHexRequest, IBitCloutPost>(r => r
                .Comment(0, 100)
                .FetchParents(true)
                .PostHashHex(target.Hex)
                .ReaderPublicKeyBase58Check("BC1YLgQfRv7rpS4qo1jmS8aLq1a8wSE2VenGzHFG9zRyd6Eu5Y8jjca"));

            return post
                .GetComments()
                .OrderByDescending(c => c.TimeStamp)
                .FirstOrDefault(comment =>
                    comment.PosterPublicKeyBase58Check.Equals(promoter.PublicKey) &&
                    comment.TimeStamp >= promoStart
                );
        }

        protected internal async Task<IBitCloutPost> FindTargetPost(IBitCloutUser promoter, IPromotionTarget target, PromotionActivity activity, DateTimeOffset promoStart)
        {
            var counter = 0;
            var canFetch = true;

            IBitCloutPost match = null;

            var req = new GetPostsForUserRequest {FetchDepth = 100, TargetUser = promoter};
            while (canFetch)
            {
                var posts = await _mediator.Send(req);

                foreach (var p in posts.Where(p => p.ReCloutedPostEntry != null))
                {
                    var rc = p.ReCloutedPostEntry;

                    if (p.TimeStamp < promoStart)
                    {
                        if (match != null) return match;
                        
                        _log.Info("Scan went past creation time of target");
                        canFetch = false;
                        break;
                    }

                    if (rc == null) continue;
                    if (!rc.PostHashHex.Equals(target.Hex)) continue;
                    if (p.IsHidden) continue;

                    if (activity == PromotionActivity.Quote && IsPostAQuote(p)) return p;
                    if (activity == PromotionActivity.ReClout && IsPostAReClout(p)) return p;
                    
                    match = p;
                }

                counter += posts.Count;
                _log.Info($"Fetched {counter} posts");

                canFetch = canFetch && (posts.Count == 100);
                req.StartFromPostHex = posts.LastOrDefault()?.PostHashHex ?? "";
            }

            if (match == null && counter == 0)
                throw new CloutCastException(new ErrorModel
                {
                    Data = {["Promoter"] = promoter},
                    Message = "No posts found for user on block chain",
                    StatusCode = (int) HttpStatusCode.NotFound
                });

            return match;
        }

        protected override void SetupValidation(HandlerValidator v)
        {
            v.RuleFor(x => _action)
                .Must(x => x != PromotionActivity.Undefined)
                .WithMessage("Target Action must be defined");

            v.RuleFor(x => _match)
                .NotNull()
                .WithMessage("Proof of work not found within the promotion time frame")
                .ChildRules(y =>
                {
                    y.When(x => _action == PromotionActivity.ReClout, () => y
                        .RuleFor(x => _match)
                        .Must(m => IsPostAReClout(m) || IsPostAQuote(m))
                        .WithMessage("Found a Comment, expected ReClout")
                    );
                    y.When(x => _action == PromotionActivity.Quote, () => y
                        .RuleFor(x => _match)
                        .Must(IsPostAQuote)
                        .WithMessage("Found a ReClout, expected Quote")
                    );
                });
        }
    }
}