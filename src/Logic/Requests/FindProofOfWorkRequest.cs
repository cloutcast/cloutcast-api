using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    using Entities;

    public class FindProofOfWorkRequest : ValidatedRequest<FindProofOfWorkRequest, IBitCloutPost>
    {
        public IBitCloutUser Promoter { get; set; }
        public Promotion Promotion { get; set; }

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleSet("initialRequest", () =>
            {
                v.RuleFor(req => req.Promoter).NotNull().BitCloutUser();
                v.RuleFor(req => req.Promotion).NotNull();
                v.RuleFor(req => req.Promotion).Must(p => p.Id > 0).WithMessage("Id must be greater than 0");
                v.RuleFor(req => req.Promotion.Target).NotNull();
                v.RuleFor(req => req.Promotion.Target)
                    .Must(t => t.Action != PromotionActivity.Undefined)
                    .WithMessage("Target Action must be defined");
            });
        }
    }
}