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
    public class PayOutWorkHandler : IRequestHandler<PayOutWorkRequest>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;
        
        public PayOutWorkHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public async Task<Unit> Handle(PayOutWorkRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            if (NeedToPayout(request.LedgerItems))
                PayOutPromoter(request.LedgerItems, request.AsOf);
            
            return new Unit();
        }

        protected internal bool NeedToPayout(List<GeneralLedgerItem> ledgerItems) => ledgerItems != null && !ledgerItems.None();

        protected internal void PayOutPromoter(List<GeneralLedgerItem> powEvents, DateTimeOffset now)
        {
            try
            {
                const int flushCount = 20;
                foreach (var (pow, i) in powEvents.WithIndex())
                {
                    _pipeline.Command<IPayoutUserWorkCommand>(c => c.AsOf(now).ProofOfWorkLedger(pow));
                    _log.Info($"PayOut {pow.Credit.Describe().Replace("To", "from")}");

                    if (i % flushCount != 0) continue;
                    _log.Info($"Attempting to flush out {flushCount} payouts");
                    _pipeline.UseIsolationLevel(IsolationLevel.Snapshot).Run();
                }

                // Clean up any left overs
                _pipeline.UseIsolationLevel(IsolationLevel.Snapshot).Run();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }
        }
    }
}