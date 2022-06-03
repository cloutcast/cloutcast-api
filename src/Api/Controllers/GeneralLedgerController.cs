using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CloutCast.Controllers
{
    using Contracts;
    using Commands;
    using Entities;
    using Models;
    using Queries;
    using Requests;

    [ApiController, Route("[controller]")]
    public class GeneralLedgerController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _logger;
        
        public GeneralLedgerController(IMediator mediator, IDapperPipeline pipeline, ILog logger)
        {
            _mediator = mediator;
            _pipeline = pipeline;
            _logger = logger;
        }

        /// <summary>
        /// Record the user requested withdrawal amount
        /// </summary>
        /// <param name="userId">CloutCast UserId</param>
        /// <param name="amount">Amount in BitClout nanos to withdraw</param>
        [HttpGet, Route("{userId:long}/withdraw/{amount:long}")]
        public async Task<AccountBalanceModel> RequestWithdrawal(long userId, long amount)
        {
            _logger.Info($"Attempt to withdraw {amount} for User; UserId={userId}");
            var user = await _mediator.Send<GetUserRequest, BitCloutUser>(r => r
                .UserId(userId)
                .IncludeFollowerCount(false)
                .IncludeProfile(false)
            );

            var apps = await _mediator.Send<GetAllAppSourcesRequest, List<AppSource>>();
            var systemApp = apps.SingleOrDefault(a => a.Name == "System");

            return await _mediator.Send<WithdrawRequest, AccountBalanceModel>(r =>
            {
                r.App = systemApp;
                r.Amount = amount;
                r.User = user;
            });
        }

        /// <summary>
        /// Scan BitClout for payment records
        /// </summary>
        [HttpGet, Route("scan/out")]
        public async Task<bool> ScanForMoneyOut()
        {
            _logger.Info("Resolve any CashOuts without Receipt");

            await _mediator.Send<RecordMoneyOutRequest>(mr => mr.BitCloutMoneyOut = ScanBitClout());
            
            return true;
        }

        /// <summary>
        /// Scan BitClout for new Funding actions to record to General Ledger
        /// </summary>
        [HttpGet, Route("scan")]
        public async Task<bool> ScanForMoneyIn()
        {
            _logger.Info("Scan BitClout for funding transactions");
            var transactions = await ScanBitClout();

            var incoming = transactions.OfType<BitCloutIncomingFunds>().ToList();

            _logger.Info($"Process {incoming.Count} incoming fund transactions");
            await _mediator.Send<RecordMoneyInRequest>(r => r.IncomingFunds = incoming);

            return true;
        }

        /// <summary>
        /// Used by *Jason* to save a receipt against Cash-out transaction
        /// </summary>
        /// <remarks>
        /// Will only work against a Cash OUT GL transaction without a receipt
        /// </remarks>
        /// <param name="glId">Unique General Ledger Id</param>
        /// <param name="receiptHex">the value of the matching TransactionIDBase58Check</param>
        [HttpPost, Route("{glId}/receipt/{receiptHex}")]
        public List<GeneralLedgerItem> SaveReceipt(long glId, string receiptHex)
        {
            List<GeneralLedgerItem> results = null;
            _pipeline
                .Command<SaveReceiptCommand>(c =>
                {
                    c.GeneralLedgerId = glId;
                    c.EvidencePostHex = receiptHex;
                })
                .Query<IGetGLQuery, List<GeneralLedgerItem>>(
                    q => q.ForGeneralLedgerId(glId) ,
                    r => results = r)
                .UseIsolationLevel(IsolationLevel.Snapshot)
                .Run();

            return results;
        }

        private Task<List<IBitCloutFundingTransaction>> ScanBitClout() => _mediator
            .Send<BitCloutTransactionScanRequest, List<IBitCloutFundingTransaction>>(r =>
                r.WalletPublicKey = "BC1YLiVetFBCYjuHZY5MPwBSY7oTrzpy18kCdUnTjuMrdx9A22xf5DE");
    }
}