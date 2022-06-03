using System.Collections.Generic;
using System.Data;
using System.Linq;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Contracts;
    using Entities;
    using Queries;
    using Requests;

    public class GetInboxPromotionsForUserHandler : RequestHandler<GetInboxPromotionsForUserRequest, List<Promotion>>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public GetInboxPromotionsForUserHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        protected override List<Promotion> Handle(GetInboxPromotionsForUserRequest request) => 
            RunQuery(request.ActiveUser(), request.Active());

        protected List<Promotion> RunQuery(IBitCloutUser inboxUser, ActiveFlag flag)
        {
            List<Promotion> results = null;

            _pipeline
                .Query<IGetMatchingPromotionsByQuery, List<Promotion>>(
                    q => _log.Info(q.Active(flag).InboxUser(inboxUser)),
                    r => results = r?.ToList(),
                    () => _log.Info("No Promotions found"))
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return results;
        }
    }
}