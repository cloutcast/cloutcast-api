using System.Collections.Generic;
using System.Linq;

namespace CloutCast.Queries
{
    using Entities;

    public interface IGetAllAppSourcesQuery : IDapperQuery<List<AppSource>> { }
    public class GetAllAppSourcesQuery : DapperQuery<List<AppSource>>, IGetAllAppSourcesQuery
    {
        public override void Build(IStatementBuilder builder) => builder.Add($"select Id, ApiKey, Company, Name from {Tables.App}");

        public override List<AppSource> Read(IDapperGridReader reader) => reader.Read<AppSource>().ToList();
    }
}