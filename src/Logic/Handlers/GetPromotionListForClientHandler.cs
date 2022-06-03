using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Entities;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class GetPromotionListForClientHandler : IRequestHandler<GetPromotionListForClientRequest, List<Promotion>>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public GetPromotionListForClientHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public async Task<List<Promotion>> Handle(GetPromotionListForClientRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var clientKey = request.ClientKey();
            var activityFlag = request.Active();

            return RunQuery(clientKey, activityFlag);
        }
        
        protected List<Promotion> RunQuery(string clientKey, ActiveFlag flag)
        {
            List<Promotion> results = null;

            _pipeline
                .Query<IGetMatchingPromotionsByQuery, List<Promotion>>(
                    q => _log.Info(q.Active(flag).ClientKey(clientKey)),
                    r => results = r?.ToList(),
                    () => _log.Info("No Promotions found"))
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return results;
        }
    }
}