using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Commands;
    using Entities;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class RefundWorkHandler : IRequestHandler<RefundWorkRequest>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public RefundWorkHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public async Task<Unit> Handle(RefundWorkRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            if (NeedToRefund(request.LedgerItems))
                RefundClient(request.LedgerItems, request.AsOf);
            
            return new Unit();
        }

        protected internal bool NeedToRefund(List<GeneralLedgerItem> ledgerItems) => ledgerItems != null && !ledgerItems.None();

        protected internal void RefundClient(List<GeneralLedgerItem> powEvents, DateTimeOffset now)
        {
            const int flushCount = 20;
            foreach (var (pow, i) in powEvents.WithIndex())
            {
                _pipeline.Command<IRefundUserWorkCommand>(c => c.AsOf(now).ProofOfWorkLedger(pow));
                _log.Info($"Refund {pow.Credit.Describe().Replace("To", "from")} for Promotion[{pow.Debit.Id}]");

                if (i % flushCount != 0) continue;
                _log.Info($"Attempting to flush out {flushCount} refunds");
                _pipeline.UseIsolationLevel(IsolationLevel.Snapshot).Run();
            }

            // Clean up any left overs
            _pipeline.UseIsolationLevel(IsolationLevel.Snapshot).Run();
        }

    }
}