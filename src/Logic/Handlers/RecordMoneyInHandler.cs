using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Commands;
    using Entities;
    using Models;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class RecordMoneyInHandler : IRequestHandler<RecordMoneyInRequest>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public RecordMoneyInHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public Task<Unit> Handle(RecordMoneyInRequest request, CancellationToken cancellationToken)
        {
            //Validate request
            request.ValidateAndThrow();

            var existing = GetExistingFunds();
            var onlyNewFunding = GetNewFundingTransactions(request.IncomingFunds, existing);

            if (onlyNewFunding.None())
            {
                _log.Info("No new income funding transactions to process");
                return Unit.Task;
            }

            var userFunds = GroupFundsByUser(onlyNewFunding);
            var publicKeys = userFunds.Keys;
            var users = GetUsersFromDatabase(publicKeys);

            _pipeline
                .Command<IRecordMoneyInCommand>(c =>
                {
                    foreach (var kvp in userFunds)
                    {
                        var user = users.FirstOrDefault(u => u.PublicKey == kvp.Key);
                        if (user == null)
                        {
                            _log.Error($"User not found in processing; UserKey={kvp.Key}");
                            continue;
                        }

                        c.Fund(user, kvp.Value);
                    }
                })
                .UseIsolationLevel(IsolationLevel.Snapshot)
                .Run();

            return Unit.Task;
        }

        protected Dictionary<string, List<BitCloutIncomingFunds>> GroupFundsByUser(IEnumerable<BitCloutIncomingFunds> funds) => 
            funds.GroupBy(f => f.UserPublicKey).ToDictionary(g => g.Key, g => g.ToList());

        protected List<BitCloutIncomingFunds> GetExistingFunds()
        {
            List<BitCloutIncomingFunds> existing = null;
            _pipeline
                .Query<GetExistingIncomeQuery, List<BitCloutIncomingFunds>>(r => existing = r)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return existing ?? new List<BitCloutIncomingFunds>();
        }

        protected List<BitCloutIncomingFunds> GetNewFundingTransactions(
            List<BitCloutIncomingFunds> source,
            List<BitCloutIncomingFunds> existing) =>
            source.Except(existing).ToList();

        protected List<BitCloutUser> GetUsersFromDatabase(IEnumerable<string> publicKeys)
        {
            List<BitCloutUser> users = null;
            _pipeline
                .Query<IGetUsersByQuery, List<BitCloutUser>>(
                    q => q
                        .PublicKeys(publicKeys)
                        .IncludeProfile(false)
                        .IncludeUnRegisteredUsers(true)
                        .SaveUnRegisteredUsers(true),
                    r => users = r)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return users;
        }

    }
}