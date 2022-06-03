using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    using Models;

    public class WithdrawRequest : ValidatedRequest<WithdrawRequest, AccountBalanceModel>
    {
        public IAppSource App { get; set; }
        public IBitCloutUser User { get; set; }
        public long Amount { get; set; }

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleSet("initialRequest", () =>
            {
                v.RuleFor(req => req.App).NotNull().Validate();
                v.RuleFor(req => req.Amount).GreaterThan(0);
                v.RuleFor(req => req.User).BitCloutUser();
            });
        }
    }
}