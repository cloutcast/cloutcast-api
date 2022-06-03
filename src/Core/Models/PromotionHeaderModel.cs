using FluentValidation;

namespace CloutCast.Models
{
    using Contracts;

    public class PromotionHeaderModel: IPromotionHeader
    {
        public decimal BitCloutToUsdRate { get; set; } //    let nanosInUSD = bitCloutInUSD * 0.000000001;
        public int Duration { get; set; } //seconds
        public long Engagements { get; set; }
        public long Fee { get; set; }
        public long Rate { get; set; } // how much to pay Promoter
        
        public M Clone<M>() where M : PromotionHeaderModel, new() => new M
        {
            BitCloutToUsdRate = BitCloutToUsdRate,
            Duration = Duration,
            Engagements = Engagements,
            Fee = Fee,
            Rate = Rate
        };
    }

    public class PromotionHeaderValidator : AbstractValidator<PromotionHeaderModel>
    {
        public PromotionHeaderValidator()
        {
            RuleSet("create", () =>
            {
                RuleFor(ph => ph.BitCloutToUsdRate).GreaterThan(0);
                RuleFor(ph => ph.Engagements).GreaterThan(0);
                RuleFor(ph => ph.Duration).InclusiveBetween(30, 2880)
                    .WithMessage("Duration can only be 30 minutes to 2 days long");
                RuleFor(ph => ph.Fee).GreaterThanOrEqualTo(0);
                RuleFor(ph => ph.Rate).GreaterThan(0);
            });
        }
    }
}