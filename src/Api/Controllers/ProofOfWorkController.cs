using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloutCast.Controllers
{
    using Contracts;
    using Entities;
    using Models;
    using Requests;

    [ApiController, Route("pow")]
    public class ProofOfWorkController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILog _logger;

        public ProofOfWorkController(IMediator mediator, ILog logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Scan for the correct block on the block-chain and record ProofOfWork
        /// </summary>
        /// <param name="promotionId">CloutCast promotion Id</param>
        [Authorize, HttpPost, Route("{promotionId:long}")]
        public async Task<AccountBalanceModel> ProofOfWork(long promotionId)
        {
            var session = this.GetSession();

            var promotion = await _mediator.Send<GetPromotionRequest, Promotion>(r => r.PromotionId(promotionId));
            var promoter = await _mediator.Send<GetUserRequest, BitCloutUser>(r => r.User(session.User));
            
            await _mediator.Send<CheckClientToPromoterRatioRequest>(r => r
                .Client(promotion.Client)
                .Promoter(promoter)
            );

            var post = await _mediator.Send<FindProofOfWorkRequest, IBitCloutPost>(r =>
            {
                r.Promoter = promoter;
                r.Promotion = promotion;
            });

            var balance = await _mediator.Send<ProofOfWorkRequest, AccountBalanceModel>(r =>
            {
                r.App = session.App;
                r.Promotion = promotion;
                r.Promoter = promoter;
                r.ProofOfWorkPostHex = post?.PostHashHex;
            });

            if (balance.Settled < promotion.Budget())
                await _mediator.Send<StopPromotionRequest, Promotion>(r =>
                {
                    r.App = session.App;
                    r.ActiveUser = promotion.Client; // only the Client can stop a promotion
                    r.PromotionId = promotion.Id;
                });

            return balance;
        }

        [Authorize, HttpPost, Route("find/{promotionId:long}")]
        public async Task<IBitCloutPost> FindForPromotion(long promotionId)
        {
            var session = this.GetSession();

            var promotion = await _mediator.Send<GetPromotionRequest, Promotion>(r => r.PromotionId(promotionId));

            var post = await _mediator.Send<FindProofOfWorkRequest, IBitCloutPost>(r =>
            {
                r.Promoter = session.User;
                r.Promotion = promotion;
            });

            return post;
        }

        /// <summary>
        /// Validate ProofOfWork and issue funds or refunds
        /// </summary>
        [HttpGet, Route("payout")]
        public async Task PayOut()
        {
            var asOf = DateTimeOffset.UtcNow;
            var payouts = new ConcurrentQueue<GeneralLedgerItem>();
            var refunds = new ConcurrentQueue<GeneralLedgerItem>();
            var pending = await _mediator.Send<GetPendingValidationsRequest, List<GeneralLedgerItem>>(r => r.AsOf = asOf);
            
            var removed = await RemoveBlackListedUserPayouts(asOf, pending);
            var list = pending
                .Except(removed)
                .Select(item =>
                {
                    var req = new GetPostByHashHexRequest().PostHashHex(item.EvidencePostHex, false);
                    return _mediator.Send(req).ContinueWith(getTask =>
                    {
                        var post = getTask.Result;
                        if (post == null) 
                            refunds.Enqueue(item);

                        else if (post.IsHidden)
                            refunds.Enqueue(item);

                        else
                            payouts.Enqueue(item);
                    });
                })
                .ToList();
            
            Task.WaitAll(list.ToArray());

            //Publish to Database
            await _mediator.Send<PayOutWorkRequest>(r =>
            {
                r.AsOf = asOf;
                r.LedgerItems = payouts.ToList();
            });
            await _mediator.Send<RefundWorkRequest>(r =>
            {
                r.AsOf = asOf;
                r.LedgerItems = refunds.ToList();
            });
        }
        
        private async Task<IEnumerable<GeneralLedgerItem>> RemoveBlackListedUserPayouts(DateTimeOffset asOf, List<GeneralLedgerItem> pending)
        {
            var removed = new ConcurrentQueue<GeneralLedgerItem>();

            var grouping = pending.Where(p => p.Credit.IsUser()).GroupBy(p => p.Credit.BitCloutIdentifier).ToList();
            Parallel.ForEach(grouping, async grp =>
            {
                var userPayouts = grp.ToList();

                var user = await _mediator.Send<GetUserRequest, BitCloutUser>(r => r
                    .FetchFromBitClout(true)
                    .IncludeFollowerCount(false)
                    .PublicKey(grp.Key));

                if (!user.BlackList) return;

                _logger.Info($"Reject {userPayouts} payouts for blacklisted user; {user}");
                foreach (var item in userPayouts)
                    removed.Enqueue(item);
            });

            if (removed.Any())
                await _mediator.Send<RefundWorkRequest>(r =>
                {
                    r.AsOf = asOf;
                    r.LedgerItems = removed.ToList();
                });
            
            return removed;
        }
    }
}