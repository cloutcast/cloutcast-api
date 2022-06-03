using System.Collections.Generic;
using FluentValidation;

namespace CloutCast.Requests
{
    using Models;

    public class RecordMoneyInRequest : ValidatedRequest<RecordMoneyInRequest>
    {
        public List<BitCloutIncomingFunds> IncomingFunds { get; set; }

        protected override void SetupValidation(RequestValidator validator)
        {
            validator.RuleFor(req => req.IncomingFunds).NotEmpty();
        }
    }
}