using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;

    public class RecordMoneyOutRequest : ValidatedRequest<RecordMoneyOutRequest>
    {
        public Task<List<IBitCloutFundingTransaction>> BitCloutMoneyOut { get; set; }

        protected override void SetupValidation(RequestValidator validator)
        {
            validator.RuleFor(req => req.BitCloutMoneyOut).NotNull();
        }
    }
}