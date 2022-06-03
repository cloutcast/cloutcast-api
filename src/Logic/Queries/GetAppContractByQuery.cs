using System.Collections.Generic;
using System.Linq;

namespace CloutCast.Queries
{
    using Contracts;
    using Entities;
    using Models;

    public interface IGetAppContractByQuery : IDapperQuery<List<AppContractModel>>
    {
        IAppSource App { set; }
    }
    public class GetAppContractByQuery : DapperQuery<List<AppContractModel>>, IGetAppContractByQuery
    {
        public IAppSource App {get; set; }

        private static void Map(AppContractModel contract, ContractedFeeModel fee, BitCloutUser payee)
        {
            if (contract == null || fee == null) return;
            fee.Payee = payee;
            contract.Fees = new List<ContractedFeeModel> {fee};
        }

        public override List<AppContractModel> Read(IDapperGridReader reader) => reader
            .Map<AppContractModel, ContractedFeeModel, BitCloutUser>(Map)
            .GroupBy(x => x.Action)
            .Select(grp => new AppContractModel
            {
                Action = grp.Key,
                App = App,
                Fees = grp.SelectMany(g => g.Fees).ToList()
            })
            .ToList();

        public override void Build(IStatementBuilder builder)
        {
            builder.Add($@"
select 0 as Id, c.Action, 
       0 as Id, c.Percentage, 
       u.Id, u.Handle, u.PublicKey 
from {Tables.Contract} c 
inner join {Tables.User} u on u.Id = c.UserId
where c.AppId = {App.Id} ");
        }
    }
}