using FluentValidation;

namespace CloutCast.Requests
{
    using Models;

    public class CheckMinimumAcceptablePaymentRequest : ValidatedRequest<CheckMinimumAcceptablePaymentRequest>
    {
        private PromotionCriteriaModel _criteria;
        public PromotionCriteriaModel Criteria() => _criteria;
        public CheckMinimumAcceptablePaymentRequest Criteria(PromotionCriteriaModel criteria) => this.Fluent(x => _criteria = criteria);

        private PromotionHeaderModel _header;
        public PromotionHeaderModel Header() => _header;
        public CheckMinimumAcceptablePaymentRequest Header(PromotionHeaderModel header) => this.Fluent(x => _header = header);

        protected override void SetupValidation(RequestValidator validator)
        {
            validator.RuleFor(req => req._header).NotNull().SetValidator(new PromotionHeaderValidator());
            validator.RuleFor(req => req._criteria).NotNull().SetValidator(new PromotionCriteriaValidator());
        }
    }
}