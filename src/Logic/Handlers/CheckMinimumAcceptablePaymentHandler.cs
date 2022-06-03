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
    public class CheckMinimumAcceptablePaymentHandler : IRequestHandler<CheckMinimumAcceptablePaymentRequest>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public CheckMinimumAcceptablePaymentHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public async Task<Unit> Handle(CheckMinimumAcceptablePaymentRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var sourceUsers = request.Criteria().AllowedUsers;
            if (NeedToCheck(sourceUsers))
            {
                var allowedUsers = GetAllowedUsers(sourceUsers);
                ThrowOnAnyFailure(allowedUsers, request.Header());
            }
            return Unit.Value;
        }

        protected List<BitCloutUser> GetAllowedUsers(List<string> allowedUsers)
        {
            List<BitCloutUser> users = null;
            _pipeline
                .Query<IGetUsersByQuery, List<BitCloutUser>>(
                    q => q
                        .PublicKeys(allowedUsers)
                        .IncludeProfile(true)
                        .IncludeUnRegisteredUsers(false)
                        .SaveUnRegisteredUsers(false),
                    u => users = u)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();
            return users;
        }

        protected bool NeedToCheck(List<string> allowedUsers) => allowedUsers != null && allowedUsers.Any();

        protected void ThrowOnAnyFailure(IEnumerable<BitCloutUser> allowedUsers, PromotionHeaderModel header)
        {
            var error = new ErrorModel
            {
                Message = "Promotion Rate is less than Minimum Acceptable Payment",
                StatusCode = (int) HttpStatusCode.NotAcceptable
            };

            foreach (var allowedUser in allowedUsers)
            {
                var minPaymentAmount = allowedUser.Profile.MinimumAcceptablePayment();
                if (minPaymentAmount <= 0) continue;
                if (minPaymentAmount > (ulong) header.Rate)
                {
                    error.Data[allowedUser.PublicKey] = new
                    {
                        allowedUser.Handle,
                        MinPaymentAmount = minPaymentAmount
                    };
                }
            }

            if (error.Data.Any())
                throw new CloutCastException(error);
        }
    }
}