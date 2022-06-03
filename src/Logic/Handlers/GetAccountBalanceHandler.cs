using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Models;
    using Queries;
    using Requests;

    [UsedImplicitly]
    public class GetAccountBalanceHandler : IRequestHandler<GetAccountBalanceRequest, AccountBalanceModel>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public GetAccountBalanceHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public async Task<AccountBalanceModel> Handle(GetAccountBalanceRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var owner = request.AccountOwner();
            switch (owner.Type)
            {
                case GeneralLedgerAccountType.Promotion:
                    return GetPromotionBalance(owner.Id, request.AsOf());

                case GeneralLedgerAccountType.User:
                    return GetUserBalance(owner.Id, request.AsOf());
            }

            return null;
        }

        protected AccountBalanceModel GetPromotionBalance(long promotionId, DateTimeOffset? asOf)
        {
            AccountBalanceModel balance = null;
            _pipeline
                .Query<GetBalanceForPromotionQuery, AccountBalanceModel>(
                    q => q.PromotionId(promotionId).AsOf(asOf),
                    b => balance = b)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return balance;

        }

        protected AccountBalanceModel GetUserBalance(long userId, DateTimeOffset? asOf)
        {
            AccountBalanceModel balance = null;
            _pipeline
                .Query<GetBalanceForUserQuery, AccountBalanceModel>(
                    q =>
                    {
                        q.UserId(userId);
                        if (asOf != null) q.AsOf(asOf.Value);
                    },
                    b => balance = b)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return balance;
        }
    }
}