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
    using Entities;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class StopPromotionHandler : IRequestHandler<StopPromotionRequest, Promotion>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public StopPromotionHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public async Task<Promotion> Handle(StopPromotionRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken, "initialRequest");

            var promotionId = request.PromotionId;
            request.Selected = GetPromotionFromDb(promotionId);
            await request.ValidateAndThrowAsync(cancellationToken, "isStopAllowed");

            UpdateDatabase(request.App, request.Selected, DateTimeOffset.UtcNow);
            return GetPromotionFromDb(promotionId);
        }

        protected internal Promotion GetPromotionFromDb(long promotionId)
        {
            Promotion result = null;
            _pipeline
                .Query<IGetPromotionByIdQuery, Promotion>(q => q.PromotionId(promotionId), p => result = p)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();
            return result;
        }

        protected internal void UpdateDatabase(IAppSource app, Promotion source, DateTimeOffset stopOn)
        {
            var client = source.Client;
            var expired = source.Events.Expired();

            _pipeline
                .Command<IAppendToEntityLogCommand>(c => c
                    .Log(EntityAction.PromotionStop, client.Id, source.Id)
                    .OutputParam("StopEventId")
                    .AsOf(stopOn, app))

                //Move all GL entries @ expiration time to the StopPromotion time
                .Command<ICopyGeneralLedgerCommand>(c => c
                    .SourceId(expired.Id)
                    .TargetParam("StopEventId"))

                //Back out all the expired ledger entries
                .Command<IReverseGeneralLedgerCommand>(c => c
                    .SourceId(expired.Id)
                    .TargetId(expired.Id))

                .UseIsolationLevel(IsolationLevel.Snapshot);
        }
    }
}