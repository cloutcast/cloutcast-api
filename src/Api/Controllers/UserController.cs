using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloutCast.Controllers
{
    using Entities;
    using Models;
    using Requests;

    [ApiController, Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILog _logger;

        public UserController(IMediator mediator, ILog logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        
        /// <summary>
        /// Returns the current GeneralLedger Totals for the given user
        /// </summary>
        /// <param name="userId">CloutCast UserId</param>
        [HttpGet, Route("balance/{userId:long}")]
        public async Task<AccountBalanceModel> Balance(long userId)
        {
            _logger.Info($"Return Balance for User; UserId={userId}");
            return await _mediator.Send<GetAccountBalanceRequest, AccountBalanceModel>(r =>
                r.AccountOwner(GeneralLedgerAccountType.User, userId));
        }

        [HttpGet, Route("balance/{userId:long}/{asOf:datetime}")]
        public async Task<AccountBalanceModel> Balance(long userId, DateTime asOf)
        {
            _logger.Info($"Return Balance for User; UserId={userId}");
            return await _mediator.Send<GetAccountBalanceRequest, AccountBalanceModel>(r =>
                r
                    .AccountOwner(GeneralLedgerAccountType.User, userId)
                    .AsOf(asOf)
                );
        }
        /// <summary>
        /// Returns the CloutCast User record for the given userId
        /// </summary>
        /// <param name="userId">CloutCast UserId</param>
        [HttpGet, Route("{userId:long}")]
        public async Task<BitCloutUser> Get(long userId)
        {
            _logger.Info($"Return User for UserId={userId}");
            return await _mediator.Send<GetUserRequest, BitCloutUser>(r => r.UserId(userId));
        }

        /// <summary>
        /// Returns the CloutCast User record for the given BitClout Public Key
        /// </summary>
        /// <param name="publicKey">BitClout Public UserKey</param>
        [HttpGet, Route("publicKey/{publicKey}")]
        public async Task<BitCloutUser> Get(string publicKey)
        {
            _logger.Info($"Return User for PublicKey={publicKey}");
            return await _mediator.Send<GetUserRequest, BitCloutUser>(r => r.PublicKey(publicKey));
        }

        /// <summary>
        /// Returns list of Promotions available for the active user
        /// </summary>
        [Authorize, HttpGet, Route("inbox")]
        public async Task<List<Promotion>> GetInboxForUser()
        {
            var session = this.GetSession();
            return await _mediator.Send<GetInboxPromotionsForUserRequest, List<Promotion>>(r => r
                .Active(ActiveFlag.Active)
                .ActiveUser(session.User));
        }

        /// <summary>
        /// Returns the count of active promotions available for the given user
        /// </summary>
        /// <param name="publicKey">BitClout Public UserKey</param>
        [HttpGet, Route("inbox/count/{publicKey}")]
        public async Task<long> GetInboxCountForUser(string publicKey)
        {
            var user = await _mediator.Send<GetUserRequest, BitCloutUser>(r => r.PublicKey(publicKey));
            var count = await _mediator.Send<GetInboxPromotionCountForUserRequest, long>(r => r
                .Active(ActiveFlag.Active)
                .ActiveUser(user));
            return count;
        }
        
        /// <summary>
        /// Marks promotion as 'read' for user inbox
        /// </summary>
        /// <param name="promotionId">Promotion Id</param>
        [HttpGet, Route("inbox/read/{promotionId:long}")]
        public async Task<Promotion> MarkAsRead(long promotionId)
        {
            var user = this.GetSession()?.User;
            var userId = user == null ? "user not found" : $"{user.Id}";
            _logger.Info($"Mark Promotion as read for User; PromotionId={promotionId}; UserId={userId};");

            return await _mediator.Send<MarkPromotionAsReadForUserRequest, Promotion>(r => r
                .PromotionId(promotionId)
                .User(user));
        }
        
        /// <summary>
        /// Returns the full ledger events for a given User
        /// </summary>
        /// <param name="userId">CloutCast UserId</param>
        [HttpGet, Route("ledger/{userId:long}")]
        public async Task<List<GeneralLedgerItem>> Ledger(long userId)
        {
            _logger.Info($"Return General Ledger for User; UserId={userId}");
            return await _mediator.Send<GetGLByRequest, List<GeneralLedgerItem>>(
                r => r.AccountOwner(GeneralLedgerAccountType.User, userId));
        }
    }
}