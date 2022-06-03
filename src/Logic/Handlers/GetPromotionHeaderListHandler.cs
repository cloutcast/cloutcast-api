using System.Collections.Generic;
using System.Data;
using System.Linq;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Entities;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class GetPromotionHeaderListHandler : RequestHandler<GetPromotionHeaderListRequest, List<Promotion>>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public GetPromotionHeaderListHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        protected override List<Promotion> Handle(GetPromotionHeaderListRequest request) => RunQuery(request.Active());

        protected List<Promotion> RunQuery(ActiveFlag flag)
        {
            List<Promotion> results = null;

            _pipeline
                .Query<IGetMatchingPromotionsByQuery, List<Promotion>>(
                    q => _log.Info(q
                        .Active(flag)
                        .IncludeEntityLogs(false)
                        .IncludePromoters(true)),
                    r => results = r?.ToList(),
                    () => _log.Info("No Promotions found"))
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return results;
        }
    }
}