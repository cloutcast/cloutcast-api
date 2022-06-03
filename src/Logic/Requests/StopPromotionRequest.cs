using System.Net;
using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    using Entities;

    public class StopPromotionRequest : ValidatedRequest<StopPromotionRequest, Promotion>
    {
        public IAppSource App { get; set; }
        public IBitCloutUser ActiveUser { get; set; }
        public long PromotionId { get; set; }
        protected internal Promotion Selected { get; set; }

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleSet("initialRequest", () =>
            {
                v.RuleFor(req => req.App).NotNull().Validate();
                v.RuleFor(req => req.ActiveUser).BitCloutUser();
                v.RuleFor(r => r.PromotionId).GreaterThan(0);
            });

            v.RuleSet("isStopAllowed", () =>
            {
                v
                    .RuleFor(r => r.Selected).NotNull()
                    .WithMessage($"Promotion {PromotionId} not found")
                    .WithState(x => HttpStatusCode.NotFound);

                v.RuleFor(r => r.Selected).IsActive();
                v
                    .RuleFor(r => r.Selected.Client.Id).Equal(x => x.ActiveUser.Id)
                    .WithMessage("Only the Client can stop the promotion");
            });
        }
    }
}