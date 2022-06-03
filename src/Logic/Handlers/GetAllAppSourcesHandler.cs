using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Entities;
    using Queries;
    using Requests;

    public class GetAllAppSourcesHandler : IRequestHandler<GetAllAppSourcesRequest, List<AppSource>>
    {
        private const string Key = "AllAppSource";

        private readonly MemoryCache _cache = MemoryCache.Default;
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public GetAllAppSourcesHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public Task<List<AppSource>> Handle(GetAllAppSourcesRequest request, CancellationToken cancellationToken)
        {
            var results = GetFromCache();;
            if (results == null)
            {
                results = GetFromDatabase();
                Save(results);
            }
            return Task.FromResult(results);
        }

        protected List<AppSource> GetFromCache() => _cache.Get(Key) as List<AppSource>;

        protected List<AppSource> GetFromDatabase()
        {
            _log.Info("Get all AppSources from Database");
            List<AppSource> results = null;
            _pipeline
                .Query<IGetAllAppSourcesQuery, List<AppSource>>(q => { }, r => results = r)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return results;
        }

        protected void Save(List<AppSource> results)
        {
            var expire = DateTimeOffset.UtcNow.AddDays(1);
            _cache.Add(Key, results, expire);
        }
    }
}