using FluentValidation;

namespace CloutCast.Requests
{
    using Entities;

    public class GetPromotionRequest : ValidatedRequest<GetPromotionRequest, Promotion>
    {
        private long _promotionId;
        public long PromotionId() => _promotionId;
        public GetPromotionRequest PromotionId(long promotionId) => this.Fluent(x => _promotionId = promotionId);

        private bool _onlyActive;
        public bool ThrowIfNotActive() => _onlyActive;
        public GetPromotionRequest ThrowIfNotActive(bool onlyActive) => this.Fluent(x => _onlyActive = onlyActive);

        protected override void SetupValidation(RequestValidator v) => v.RuleFor(r => r._promotionId).GreaterThan(0);
    }
}