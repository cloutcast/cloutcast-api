using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    using Models;

    public class AddFundsToUserRequest : ValidatedRequest<AddFundsToUserRequest, AccountBalanceModel>
    {
        private IAppSource _app;
        private long _amount;
        private long _userId;

        public void Funding(IAppSource app, long amount, long userId)
        {
            _app = app;
            _amount = amount;
            _userId = userId;
        }

        public IAppSource App() => _app;
        public long Amount() => _amount;
        public long UserId() => _userId;

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(req => _app).NotNull().Validate();
            v.RuleFor(req => _amount).GreaterThan(0);
            v.RuleFor(req => _userId).GreaterThan(0);
        }
    }
}