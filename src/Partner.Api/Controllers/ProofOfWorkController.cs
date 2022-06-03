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

    [ApiController, Route("[controller]")]
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

            //Find matching pow post
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
    }
}