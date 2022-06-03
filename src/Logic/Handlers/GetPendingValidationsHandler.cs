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
    public class GetPendingValidationsHandler : RequestHandler<GetPendingValidationsRequest, List<GeneralLedgerItem>>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public GetPendingValidationsHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        protected override List<GeneralLedgerItem> Handle(GetPendingValidationsRequest request)
        {
            _log.Info("Get all Pending WorkValidations");

            List<GeneralLedgerItem> result = null;
            _pipeline
                .Query<IGetGLQuery, List<GeneralLedgerItem>>(
                    q => q.ForPendingPayouts(request.AsOf),
                    r => result = r.ToList()
                )
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return result;
        }
    }
}