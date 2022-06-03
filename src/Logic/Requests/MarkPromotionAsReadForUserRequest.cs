using FluentValidation;
using PureLogicTek;

namespace SkunkWerx.CloutCast.Requests
{
    using Contracts;
    using Entities;

    public class MarkPromotionAsReadForUserRequest : ValidatedRequest<MarkPromotionAsReadForUserRequest, Promotion>
    {
        private long _promotionId;
        private IBitCloutUser _user;

        public MarkPromotionAsReadForUserRequest PromotionId(long promotionId) => this.Fluent(x => _promotionId = promotionId);
        public long PromotionId() => _promotionId;
        
        public MarkPromotionAsReadForUserRequest User(IBitCloutUser user) => this.Fluent(x => _user = user);
        public IBitCloutUser User() => _user;

        protected override void SetupValidation(RequestValidator validator)
        {
            validator.RuleFor(req => req._user).NotNull().BitCloutUser();
            validator.RuleFor(req => req._user).Must(bu => !bu.BlackList).WithMessage("User not allowed");
            validator.RuleFor(req => req._promotionId).Must(pId => pId > 0).WithMessage("Id must be greater than 0");
        }
    }
}