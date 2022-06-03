using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    using Entities;
    using Models;

    public class ProofOfWorkRequest : ValidatedRequest<ProofOfWorkRequest, AccountBalanceModel>
    {
        public IAppSource App { get; set; }
        public IBitCloutUser Promoter { get; set; }
        public Promotion Promotion { get; set; }
        public string ProofOfWorkPostHex { get; set; }

        protected override void SetupValidation(RequestValidator validator)
        {
            validator
                .RuleFor(req => req.App)
                .NotNull()
                .Validate();

            validator
                .RuleFor(req => req.ProofOfWorkPostHex)
                .NotEmpty()
                .WithMessage("Proof of work not found within the promotion time frame");

            validator.RuleFor(req => req.Promoter).NotNull().BitCloutUser();
            validator.RuleFor(req => req.Promoter).Must(bu => !bu.BlackList).WithMessage("User not allowed");
            validator.RuleFor(req => req.Promotion).NotNull();
            validator.RuleFor(req => req.Promotion).Must(p => p.Id > 0).WithMessage("Id must be greater than 0");
            validator.RuleFor(req => req.Promotion).IsActive();
            validator.RuleFor(req => req.Promotion)
                .Must(p => p.Promoters.None(u => u.Id == Promoter.Id))
                .WithMessage("User has already performed promotion");

            validator
                .RuleFor(x => x.Promotion.Criteria)
                .Must(c => c.IsUserAllowedAsPromoter(Promoter))
                .WithMessage($"{Promoter} does not meet promotion criteria");
        }
    }
}