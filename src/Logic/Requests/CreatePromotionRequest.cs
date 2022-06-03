using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    using Entities;
    using Models;

    public class CreatePromotionRequest : ValidatedRequest<CreatePromotionRequest,Promotion>
    {
        private IAppSource _app;
        private IBitCloutUser _clientUser;

        private PromotionCriteriaModel _criteria;
        private PromotionHeaderModel _header;
        private PromotionTargetModel _target;
        private IBitCloutPost _post;

        public IAppSource App() => _app;
        public IBitCloutUser Client() => _clientUser;

        public CreatePromotionRequest ClientIdentity(IBitCloutUser client, IAppSource app)
        {
            _app = app;
            _clientUser = client;
            return this;
        }

        public CreatePromotionRequest Promotion(PromotionCriteriaModel criteria, PromotionHeaderModel header, PromotionTargetModel target, IBitCloutPost post)
        {
            _criteria = criteria;
            _header = header;
            _target = target;
            _post = post;
            _target.CreationDate = _post?.TimeStamp;

            return this;
        }

        public Promotion Promotion() => new Promotion
        {
            Criteria = _criteria,
            Header = _header,
            Target = _target
        };

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(req => req._app).NotNull().Validate();
            v.RuleFor(req => req._clientUser).BitCloutUser().Must(bu => !bu.BlackList).WithMessage("User not allowed");
            v.RuleFor(req => req._criteria).SetValidator(new PromotionCriteriaValidator());
            v.RuleFor(req => req._header).SetValidator(new PromotionHeaderValidator());
            v.RuleFor(req => req._target).SetValidator(new PromotionTargetValidator());
        }
    }
}