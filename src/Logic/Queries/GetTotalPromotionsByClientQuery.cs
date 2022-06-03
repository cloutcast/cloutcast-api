using System.Linq;

namespace CloutCast.Queries
{
    public interface IGetTotalPromotionsByClientQuery : IDapperQuery<long>
    {
        IGetTotalPromotionsByClientQuery ClientId(long clientId);
    }
    public class GetTotalPromotionsByClientQuery : IGetTotalPromotionsByClientQuery
    {
        private long _clientId;
        public IGetTotalPromotionsByClientQuery ClientId(long clientId) => this.Fluent(x => _clientId = clientId);

        public void Build(IStatementBuilder builder) => builder
            .Add($@"select count(*) from Promotion p where p.UserId = {_clientId}");

        public long Read(IDapperGridReader reader) => reader.Read<long>().SingleOrDefault();
    }
}