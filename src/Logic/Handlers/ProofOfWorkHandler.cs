using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using log4net;

namespace CloutCast.Handlers
{
    using Commands;
    using Contracts;
    using Entities;
    using Models;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class ProofOfWorkHandler : ValidatedHandler<ProofOfWorkHandler, ProofOfWorkRequest, AccountBalanceModel>
    {
        const long SystemUserId = 1;
        private const string EventIdParamName = "powEventId";

        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;
        private bool _evidenceExists;
        private AppContractModel _contract;

        public ProofOfWorkHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public override async Task<AccountBalanceModel> Handle(ProofOfWorkRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var app = request.App;
            var promotion = request.Promotion;
            var promoter = request.Promoter;
            var now = DateTimeOffset.UtcNow;

            FetchContract(app);
            CheckIfEvidenceAlreadyExists(request.ProofOfWorkPostHex);
            await ValidateAndThrowAsync(cancellationToken);
            
            CheckPromotionBalance(promotion, promotion.Budget());
            PayPromoter(app, promotion, promoter, request.ProofOfWorkPostHex, now);
            var contractFees = CalculateContracts(_contract, promotion);

            SystemFee(app, contractFees, promotion, promoter, now);
            EnsureFundsAtExpiration(promotion);

            return GetPromotionBalance(promotion, now);
        }

        private long CalculatePayout(IContractedFee fee, long totalFee)
        {
            var payout = Convert.ToInt64(totalFee * (fee.Percentage / 100.0d));
            _log.Info($"Payout {payout} to {fee.Payee}");
            return payout;
        }

        protected internal Dictionary<IBitCloutUser, long> CalculateContracts(IAppContract contract, Promotion promotion)
        {
            long payout;
            long totalSpent = 0;
            var totalFee = promotion.Header.Fee;
            var result = new Dictionary<IBitCloutUser, long>();

            var overflowContract = contract.Fees.SingleOrDefault(f => f.Payee.IsSystemUser()) ??
                                   contract.Fees.Last();

            foreach (var contractedFee in contract.Fees.Where(f => f.Payee.Id != overflowContract.Payee.Id))
            {
                payout = CalculatePayout(contractedFee, totalFee);
                totalSpent += payout;
                result[contractedFee.Payee] = payout;
            }

            payout = CalculatePayout(overflowContract, totalFee);
            totalSpent += payout;
            result[overflowContract.Payee] = payout;

            var overflow = totalFee - totalSpent;
            if (overflow <= 0) return result;

            _log.Info($"Add overflow of {overflow} to {overflowContract.Payee}");
            result[overflowContract.Payee] += overflow;
            return result;
        }

        protected internal void CheckIfEvidenceAlreadyExists(string postHex)
        {
            _log.Info($"Checking if '{postHex}' is already in use");

            _pipeline
                .Query<GetEvidenceForPostHexQuery, int>(
                    q => q.PostHex = postHex,
                    r => _evidenceExists = r > 0)
                .UseIsolationLevel(IsolationLevel.Snapshot)
                .Run();
        }

        protected internal void CheckPromotionBalance(Promotion promotion, long amountToCheck) => _pipeline
            .Command<ICheckAmountCommand>(c => c
                .Amount(amountToCheck)
                .AccountOwner(GeneralLedgerAccountType.Promotion, promotion.Id)
                .Ledger(GeneralLedgerType.Payable)
                .ErrorMessage("Promotion lacks funds"))
            .UseIsolationLevel(IsolationLevel.Snapshot)
            .Run();

        protected internal void FetchContract(IAppSource app)
        {
            BitCloutUser systemUser = null;
            _pipeline
                .Query<IGetBitCloutUserQuery, BitCloutUser>(
                    q => q.UserId(SystemUserId).IncludeProfile(false),
                    r => systemUser = r)
                .Query<IGetAppContractByQuery, List<AppContractModel>>(
                    q => q.App = app,
                    r => _contract = r.LastOrDefault(ac => ac.Action == EntityAction.UserDidPromotion))
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            if (_contract != null) return;
            // If contract is missing default to CloutCast
            _contract = new AppContractModel
            {
                App = app,
                Action = EntityAction.UserDidPromotion,
                Fees = new List<ContractedFeeModel>
                {
                    new ContractedFeeModel
                    {
                        Payee = systemUser,
                        Percentage = 100
                    }
                }
            };
        }

        protected internal AccountBalanceModel GetPromotionBalance(Promotion promotion, DateTimeOffset now)
        {
            AccountBalanceModel balance = null;
            _pipeline
                .Query<IGetBalanceForPromotionQuery, AccountBalanceModel>(
                    q => q.AsOf(now).PromotionId(promotion.Id),
                    bal => balance = bal)

                .UseIsolationLevel(IsolationLevel.Snapshot)
                .Run();
            return balance;
        }

        protected internal void EnsureFundsAtExpiration(Promotion promotion)
        {
            var client = promotion.Client;
            var expired = promotion.Events.Expired();

            _pipeline
                .Command<IRecordGeneralLedgerCommand>(c => c
                    .Amount(promotion.Budget())
                    .Debit(client.Id, GeneralLedgerAccountType.User, GeneralLedgerType.Deposit)
                    .Credit(promotion.Id, GeneralLedgerAccountType.Promotion, GeneralLedgerType.Payable)
                    .EntityLogId(expired.Id)
                    .Memo("Ensure funds at expiration"));
        }

        protected internal void PayPromoter(IAppSource app, Promotion promotion, IBitCloutUser promoter, string postHex, DateTimeOffset now) => _pipeline
            .Command<IAppendToEntityLogCommand>(c => c
                .AsOf(now, app)
                .OutputParam(EventIdParamName)
                .Log(EntityAction.UserDidPromotion, promoter.Id, promotion.Id))

            .Command<IAppendToValidateWorkCommand>(c => c
                .CheckOn(DateTimeOffset.UtcNow.AddDays(2))
                .EntityLogParam(EventIdParamName))

            .Command<IRecordGeneralLedgerCommand>(c => c
                .Amount(promotion.Header.Rate)
                .Debit(promotion.Id, GeneralLedgerAccountType.Promotion, GeneralLedgerType.Payable)
                .Credit(promoter.Id, GeneralLedgerAccountType.User, GeneralLedgerType.Payable)
                .EntityLogParam(EventIdParamName)
                .Memo($"Move funds from Promotion #{promotion.Id} into escrow for {promoter.ToDescription()}")
                .ProofOfWork(postHex));


        protected internal void SystemFee(IAppSource app, Dictionary<IBitCloutUser, long> payouts, Promotion promotion, IBitCloutUser promoter, DateTimeOffset now)
        {
            _pipeline
                .Command<IAppendToEntityLogCommand>(c => c
                    .AsOf(now, app)
                    .OutputParam(EventIdParamName)
                    .Log(EntityAction.SystemFee, promoter.Id, promotion.Id));

            var memo = $"System Fee from Promotion #{promotion.Id} for {promoter.ToDescription()}";
            foreach (var kvp in payouts)
            {
                var payOut = kvp.Value;
                var payeeId = kvp.Key.Id;

                _pipeline
                    .Command<IRecordGeneralLedgerCommand>(c => c
                        .Amount(payOut)
                        .Debit(promotion.Id, GeneralLedgerAccountType.Promotion, GeneralLedgerType.Payable)
                        .Credit(payeeId, GeneralLedgerAccountType.User, GeneralLedgerType.Deposit)
                        .EntityLogParam(EventIdParamName)
                        .Memo(memo));
            }
        }

        protected override void SetupValidation(HandlerValidator validator)
        {
            var totalPercentage = _contract.Fees.Sum(f => f.Percentage);
            validator
                .RuleFor(x => totalPercentage).Equal(100)
                .WithMessage($"Total contracted percentage did not equal 100%; Percentage={totalPercentage}");

            validator
                .RuleFor(x => _evidenceExists)
                .Must(e => e == false)
                .WithMessage("BitClout post already used for Proof Of Work");
        }
    }
}