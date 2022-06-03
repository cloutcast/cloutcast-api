using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    using Entities;

    [JetBrains.Annotations.UsedImplicitly]
    public class CheckClientToPromoterRatioRequest : ValidatedRequest<CheckClientToPromoterRatioRequest>
    {
        private BitCloutUser _client;
        public BitCloutUser Client() => _client;
        public CheckClientToPromoterRatioRequest Client(BitCloutUser client) => this.Fluent(x => _client = client);

        private BitCloutUser _promoter;
        public BitCloutUser Promoter() => _promoter;
        public CheckClientToPromoterRatioRequest Promoter(BitCloutUser promoter) => this.Fluent(x => _promoter = promoter);

        protected override void SetupValidation(RequestValidator validator)
        {
            validator
                .RuleFor(req => req._client)
                .BitCloutUser()
                .Must(bu => !bu.BlackList)
                .WithMessage("User not allowed");

            validator
                .RuleFor(req => req._promoter)
                .BitCloutUser()
                .Must(bu => !bu.BlackList)
                .WithMessage("User not allowed");
        }
    }
}