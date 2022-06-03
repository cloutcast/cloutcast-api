using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Commands;
    using Contracts;
    using Models;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class WithdrawHandler : IRequestHandler<WithdrawRequest, AccountBalanceModel>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public WithdrawHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public async Task<AccountBalanceModel> Handle(WithdrawRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken, "initialRequest");

            var amount = request.Amount;
            var userId = request.User.Id;
            
            CheckBalance(amount, userId);
            RecordCashOut(request.App, amount, userId);
            return GetBalanceForUser(userId);
        }

        protected void CheckBalance(long amount, long userId) => _pipeline
            .Command<ICheckAmountCommand>(c => c
                .Amount(amount)
                .AccountOwner(GeneralLedgerAccountType.User, userId)
                .ErrorMessage("Insufficient funds available to withdraw")
                .Ledger(GeneralLedgerType.Deposit));

        protected AccountBalanceModel GetBalanceForUser(long userId)
        {
            AccountBalanceModel balance = null;
            _pipeline
                .Query<IGetBalanceForUserQuery, AccountBalanceModel>(
                    q => q.UserId(userId),
                    r => balance = r
                )
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return balance;
        }

        protected void RecordCashOut(IAppSource app, long amount, long userId) => _pipeline
            .Command<IAppendToEntityLogCommand>(c => c
                .AsOf(DateTimeOffset.UtcNow, app)
                .Log(EntityAction.UserWithdrawFunds, userId)
                .OutputParam("WithdrawalId")
            )
            .Command<RecordGeneralLedgerCommand>(c => c
                .Amount(amount)
                .Credit(userId, GeneralLedgerAccountType.User, GeneralLedgerType.Cash)
                .Debit(userId, GeneralLedgerAccountType.User, GeneralLedgerType.Deposit)
                .EntityLogParam("WithdrawalId")
            );
    }
}