using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    using Models;

    public class GetCoinPriceForUserRequest : ValidatedRequest<GetCoinPriceForUserRequest, SingleUserProfile>
    {
        public IBitCloutUser User { get; set; }

        protected override void SetupValidation(RequestValidator v) => v
            .RuleFor(req => req.User).NotNull()
            .DependentRules(() =>
                v.RuleFor(req => req.User.PublicKey).NotEmpty().WithMessage("Missing User PublicKey"));
    }
}