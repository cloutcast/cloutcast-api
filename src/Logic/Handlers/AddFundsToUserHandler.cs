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
    using Queries;
    using Models;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class AddFundsToUserHandler : IRequestHandler<AddFundsToUserRequest, AccountBalanceModel>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public AddFundsToUserHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public async Task<AccountBalanceModel> Handle(AddFundsToUserRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);
            AddFundsToDb(request.App(), request.Amount(), request.UserId());

            return GetUserBalance(request.UserId());
        }

        protected internal void AddFundsToDb(IAppSource app, long amount, long userId) => _pipeline
            .Command<IAppendToEntityLogCommand>(c => c
                .AsOf(DateTimeOffset.UtcNow, app)
                .OutputParam("FundingEventId")
                .Log(EntityAction.UserAddFunds, userId))

            .Command<IRecordGeneralLedgerCommand>(w => w
                .Amount(amount)
                .Debit(userId, GeneralLedgerAccountType.User, GeneralLedgerType.Cash)
                .Credit(userId, GeneralLedgerAccountType.User, GeneralLedgerType.Deposit)
                .Memo("Received funds")
                .EntityLogParam("FundingEventId"))

            .UseIsolationLevel(IsolationLevel.Snapshot);

        protected AccountBalanceModel GetUserBalance(long userId)
        {
            AccountBalanceModel balance = null;
            _pipeline
                .Query<IGetBalanceForUserQuery, AccountBalanceModel>(
                    q => q.UserId(userId),
                    b => balance = b)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return balance;
        }
    }
}