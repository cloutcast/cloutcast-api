using System;
using System.Net;
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
    public class PromotionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILog _logger;

        public PromotionController(IMediator mediator, ILog logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Returns 200 if active user matches the given promotion's criteria
        /// Returns 403 if promotions is not valid for user
        /// </summary>
        /// <param name="promotionId">CloutCast promotion Id</param>
        [Authorize, HttpGet, Route("allowed/{promotionId:long}")]
        public async Task<BitCloutUser> Allowed(long promotionId)
        {
            var session = this.GetSession();

            //update user
            var user = await _mediator.Send<GetUserRequest, BitCloutUser>(r => r
                .IncludeFollowerCount(true)
                .IncludeProfile(true)
                .User(session.User)
            );

            var promotion = await _mediator.Send<GetPromotionRequest, Promotion>(r => r
                .PromotionId(promotionId)
                .ThrowIfNotActive(true)
            );

            if (!promotion.Criteria.IsUserAllowedAsPromoter(user))
                throw new CloutCastException(new ErrorModel
                {
                    Data = {["user"] = user},
                    Message = "User does not match criteria",
                    StatusCode = (int) HttpStatusCode.Forbidden
                });

            await GetTargetPost(promotion.Target.Hex, user);
            return user;
        }

        /// <summary>
        /// Returns balance of Promotion based on current time
        /// </summary>
        /// <param name="promotionId">CloutCast promotion Id</param>
        [Authorize, HttpGet, Route("balance/{promotionId:long}")]
        public async Task<AccountBalanceModel> Balance(long promotionId) => await _mediator
            .Send<GetAccountBalanceRequest, AccountBalanceModel>(r => r
                .AccountOwner(GeneralLedgerAccountType.Promotion, promotionId)
                .AsOf(DateTimeOffset.UtcNow));

        /// <summary>
        /// Create a promotion based on payload data
        /// </summary>
        /// <remarks>
        /// NOTE: Still need validations to prevent creating same promotions more than once.
        /// </remarks>
        /// <param name="createModel">Payload to create a promotion</param>
        [Authorize, HttpPost, Route("create")]
        public async Task<Promotion> Create(CreatePromotionModel createModel)
        {
            // Check for existence of Active Promotion with same Target Criteria AND Client User
            var session = this.GetSession();

            var client = await _mediator.Send<GetUserRequest, BitCloutUser>(r => r
                .User(session.User)
                .IncludeProfile(true)
                .IncludeFollowerCount(false)
                .FetchFromBitClout(false));

            await _mediator.Send<CheckMinimumAcceptablePaymentRequest>(r => r
                .Criteria(createModel.Criteria)
                .Header(createModel.Header)
            );

            var post = await GetTargetPost(createModel.Target.Hex, client);

            return await _mediator.Send<CreatePromotionRequest, Promotion>(r => r
                .ClientIdentity(client, session.App)
                .Promotion(createModel.Criteria, createModel.Header, createModel.Target, post));
        }

        /// <summary>
        /// Returns the current Promotion for the given Id
        /// </summary>
        /// <param name="promotionId">CloutCast promotion Id</param>
        [AllowAnonymous, HttpGet, Route("{promotionId:long}")]
        public async Task<Promotion> Get(long promotionId)
        {
            _logger.Info($"Get Promotion; PromotionId={promotionId}");
            return await _mediator.Send<GetPromotionRequest, Promotion>(r => r.PromotionId(promotionId));
        }
        
        /// <summary>
        /// Stops a Promotion based on PromotionId
        /// </summary>
        /// <remarks>
        /// NOTE: This is only a database call. There is no error or state checking yet.
        /// </remarks>
        /// <param name="promotionId">CloutCast promotion Id</param>
        [Authorize, HttpPost, Route("stop/{promotionId:long}")]
        public async Task<Promotion> Stop(long promotionId)
        {
            var session = this.GetSession();

            return await _mediator.Send<StopPromotionRequest, Promotion>(r =>
            {
                r.App = session.App;
                r.ActiveUser = session.User;
                r.PromotionId = promotionId;
            });
        }

        private async Task<IBitCloutPost> GetTargetPost(string targetPostHex, IBitCloutUser promoter)
        {
            var post = await _mediator.Send<GetPostByHashHexRequest, IBitCloutPost>(r =>
                r.PostHashHex(targetPostHex));

            if (post == null) return null;

            var user = await _mediator.Send<GetUserRequest, BitCloutUser>(r => r
                .IncludeProfile(true)
                .PublicKey(post.PosterPublicKeyBase58Check)
                .ThrowOnNotFound(false)
            );

            if (user?.Profile == null) return post;
            if (user.Profile.AllowOthersToPromote()) return post;
            
            //Any user is always allowed to promote their own posts
            if (promoter.PublicKey.Equals(user.PublicKey)) return post;

            throw new CloutCastException(new ErrorModel
            {
                Data = {["user"] = user},
                Message = $"{user.Handle} does not allow other users to promote content",
                StatusCode = (int) HttpStatusCode.Forbidden
            });
        }
    }
}
