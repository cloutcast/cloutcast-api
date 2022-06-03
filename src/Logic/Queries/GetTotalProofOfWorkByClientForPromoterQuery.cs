using System.Collections.Generic;
using System.Linq;

namespace CloutCast.Queries
{
    using Models;

    public interface IGetTotalProofOfWorkByClientForPromoterQuery : IDapperQuery<List<TotalByClient>>
    {
        long PromoterId { get; set; }
    }
    public class GetTotalProofOfWorkByClientForPromoterQuery : IGetTotalProofOfWorkByClientForPromoterQuery
    {
        public long PromoterId { get; set; }

        public void Build(IStatementBuilder builder) => builder.Add($@"
select p.UserId as ClientId, count(*) as Total
from EntityLog el  
inner join Promotion p on p.Id = el.PromotionId
where el.Action = {(int) EntityAction.UserDidPromotion} and el.UserId = {PromoterId}
group by p.UserId");

        public List<TotalByClient> Read(IDapperGridReader reader) => reader.Read<TotalByClient>().ToList();
    }
}