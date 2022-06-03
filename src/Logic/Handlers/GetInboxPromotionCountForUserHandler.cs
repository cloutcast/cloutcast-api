using System.Data;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Contracts;
    using Queries;
    using Requests;

    public class GetInboxPromotionCountForUserHandler : RequestHandler<GetInboxPromotionCountForUserRequest, long>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public GetInboxPromotionCountForUserHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        protected override long Handle(GetInboxPromotionCountForUserRequest request) => 
            RunQuery(request.ActiveUser(), request.Active());

        protected long RunQuery(IBitCloutUser allowedUser, ActiveFlag flag)
        {
            long counter = 0;

            _pipeline
                .Query<IGetMatchingPromotionsCountByQuery, long>(
                    q => _log.Info(q.Active(flag).AllowedUser(allowedUser)),
                    r => counter = r,
                    () => _log.Info("No Promotions found"))
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return counter;
        }
    }
}