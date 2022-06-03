using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Commands;
    using Contracts;
    using Entities;
    using Models;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class RecordMoneyOutHandler : IRequestHandler<RecordMoneyOutRequest>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public RecordMoneyOutHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public Task<Unit> Handle(RecordMoneyOutRequest request, CancellationToken cancellationToken)
        {
            request.ValidateAndThrow();

            var allReceipts = GetReceiptsFromBitClout(request.BitCloutMoneyOut);
            var allCashOuts = GetAllCashOuts();
            if (allCashOuts.None()) return Unit.Task;

            RemoveResolved(allCashOuts, allReceipts);
            Process(allCashOuts, allReceipts);
            _pipeline
                .UseIsolationLevel(IsolationLevel.Snapshot)
                .Run();

            return Unit.Task;
        }

        protected List<GeneralLedgerItem> GetAllCashOuts()
        {
            List<GeneralLedgerItem> cashOuts = null;
            _pipeline
                .Query<IGetGLQuery, List<GeneralLedgerItem>>(
                    q => q.ForLedger(GeneralLedgerAction.Credit, GeneralLedgerType.Cash),
                    r => cashOuts = r)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            var count = cashOuts?.Count ?? 0;
            if (count > 0) _log.Info($"{count} pending receipt");
            return cashOuts;
        }

        protected List<BitCloutOutGoingFunds> GetReceiptsFromBitClout(Task<List<IBitCloutFundingTransaction>> fromBitClout)
        {
            if (fromBitClout == null) return new List<BitCloutOutGoingFunds>();

            fromBitClout.Wait();
            return fromBitClout.Result?.OfType<BitCloutOutGoingFunds>().ToList() ?? new List<BitCloutOutGoingFunds>();
        }

        protected void Process(List<GeneralLedgerItem> pendingGl, List<BitCloutOutGoingFunds> outGoing)
        {
            if (pendingGl.None()) return;
            foreach (var gl in pendingGl.Where(p => p.EvidencePostHex.IsEmpty() && p.Credit.Type == GeneralLedgerAccountType.User))
            {
            
                var match = outGoing
                    .Where(og => og.UserPublicKey.Trim() == gl.Credit.BitCloutIdentifier.Trim())
                    .FirstOrDefault(og =>
                    {
                        var delta = Math.Abs(gl.Amount - og.Amount);
                        return delta <= 5;
                    });
                if (match == null) continue;
                outGoing.Remove(match);

                var entityLogId = gl.EntityLog.Id;
                _log.Info($"Matched Receipt; EntityLog={entityLogId}; Amount={gl.Amount}; UserId={gl.Credit.Id}; Receipt={match.EvidencePostHex}");

                _pipeline.Command<IRecordMoneyOutCommand>(r => r.Receipt(entityLogId, match.EvidencePostHex));
            }
        }
        
        private void RemoveResolved(List<GeneralLedgerItem> cashOuts, List<BitCloutOutGoingFunds> outGoing)
        {
            var workingSet = outGoing.ToList();
            foreach (var funding in workingSet)
            {
                var matching = cashOuts
                    .Where(co => co.EvidencePostHex != null && co.EvidencePostHex.Equals(funding.EvidencePostHex))
                    .ToList();

                var count = matching.Count;
                if (count == 0) continue;

                if (matching.Count > 1)
                {
                    var reasons = new List<string> {"multiple matches found"};
                    reasons.AddRange(matching.Select(item => $"EntityLogId: {item.EntityLog?.Id ?? 0}"));

                    throw new CloutCastException(HttpStatusCode.Conflict, reasons.ToArray());
                }

                //remove already matched
                outGoing.Remove(funding);
                cashOuts.Remove(matching[0]);
            }

            //Remove already addressed cashOuts 
            foreach (var cashOut in cashOuts.Where(cashOut => !cashOut.EvidencePostHex.IsEmpty()).ToList())
                cashOuts.Remove(cashOut);
        }
    }
}