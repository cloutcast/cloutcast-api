using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using log4net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloutCast.Controllers
{
    using Contracts;
    using Entities;
    using Requests;

    [ApiController, Route("promotions")]
    public class PromotionListController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILog _logger;

        public PromotionListController(IMediator mediator, ILog logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Returns list of Promotions based on the Promotions Active state
        /// </summary>
        /// <param name="activeFlag">Active | InActive | or All</param>
        [HttpGet, Route("{activeFlag}")]
        public async Task<List<Promotion>> GetActive(ActiveFlag? activeFlag)
        {
            _logger.Info($"Get All {activeFlag ?? ActiveFlag.Both} Promotions;");
            return await _mediator.Send<GetPromotionListRequest, List<Promotion>>(r => r.Active(activeFlag));
        }

        /// <summary>
        /// Returns list of Promotions based on the Promotions Active state
        /// </summary>
        /// <param name="activeFlag">Active | InActive | or All</param>
        /// <param name="coinPrice"></param>
        /// <param name="followerCount"></param>
        [Authorize, HttpGet, Route("{activeFlag}/{coinPrice:long}/{followerCount:long}")]
        public async Task<List<Promotion>> GetActiveForUser(ActiveFlag? activeFlag, long? coinPrice, long? followerCount)
        {
            var session = this.GetSession();

            var sb = new StringBuilder()
                .AppendLine($"Get All {activeFlag ?? ActiveFlag.Both} Promotions;")
                .Append($"For User={session.User.ToDescription()};");
            if (coinPrice != null) sb.Append($"coinPrice={coinPrice};");
            if (followerCount != null) sb.Append($"followerCount={followerCount};");

            _logger.Info(sb);

            return await _mediator.Send<GetPromotionListRequest, List<Promotion>>(r => r
                .Active(activeFlag)
                .ActiveUser(session.User)
                .CoinPrice(coinPrice)
                .FollowerCount(followerCount));
        }
        
        /// <summary>
        /// Returns list of Client Promotions based on the Promotions Active state
        /// </summary>
        /// <param name="clientKey">BitClout Public Key of the client</param>
        /// <param name="activeFlag">Active | InActive | or All</param>
        [Authorize, HttpGet, Route("client/{clientKey}/{activeFlag}")]
        public async Task<List<Promotion>> GetAllForClient(string clientKey, ActiveFlag? activeFlag)
        {
            return await _mediator.Send<GetPromotionListForClientRequest, List<Promotion>>(r => r
                .Active(activeFlag)
                .ClientKey(clientKey));
        }
    }
}