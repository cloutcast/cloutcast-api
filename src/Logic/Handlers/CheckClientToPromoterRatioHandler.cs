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
    using Entities;
    using Models;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class CheckClientToPromoterRatioHandler : IRequestHandler<CheckClientToPromoterRatioRequest>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public CheckClientToPromoterRatioHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public async Task<Unit> Handle(CheckClientToPromoterRatioRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var client = request.Client();
            var promoter = request.Promoter();
            var ratio = GetRatio(client.Id, promoter.Id);

            ThrowOnAnyFailure(client, ratio);

            return Unit.Value;
        }

        private decimal GetRatio(long clientId, long promoterId)
        {
            long promotionCount = 0;
            long totalPowForClient = 0;

            _pipeline
                .Query<IGetTotalPromotionsByClientQuery, long>(
                    q => q.ClientId(clientId),
                    r => promotionCount = r
                )
                .Query<IGetTotalProofOfWorkByClientForPromoterQuery, List<TotalByClient>>(
                    q => q.PromoterId = promoterId,
                    r => totalPowForClient = r.SingleOrDefault(t => t.ClientId == clientId)?.Total ?? 0
                )
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            if (promotionCount == 0 || totalPowForClient == 0) return 0.0m;
            return (totalPowForClient * 1.0m) / (promotionCount * 1.0m);
        }

        protected void ThrowOnAnyFailure(BitCloutUser client, decimal ratio)
        {
            if (ratio < client.Profile.PromoterRatio()) return;

            throw new CloutCastException(new ErrorModel
            {
                Message = "Client ratio exceeded",
                StatusCode = (int) HttpStatusCode.Forbidden
            });
        }
    }
}