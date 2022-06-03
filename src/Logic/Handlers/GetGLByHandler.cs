using System;
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

    public class GetGLByHandler : IRequestHandler<GetGLByRequest, List<GeneralLedgerItem>>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public GetGLByHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public async Task<List<GeneralLedgerItem>> Handle(GetGLByRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var accountOwner = request.AccountOwner();

            if (accountOwner.Type == GeneralLedgerAccountType.User)
                return ByUser(accountOwner.Id);

            return null;
        }

        protected List<GeneralLedgerItem> ByUser(long userId)
        {
            _log.Info($"Get all GL Entries for User up until now; UserId={userId}");

            List<GeneralLedgerItem> result = null;
            _pipeline
                .Query<IGetGLQuery, List<GeneralLedgerItem>>(
                    q => q.ForUserId(userId, DateTimeOffset.UtcNow),
                    r => result = r.ToList()
                )
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return result;
        }
    }
}