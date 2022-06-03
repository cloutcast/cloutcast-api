using System.Collections.Generic;
using System.Linq;
using FluentValidation;


namespace CloutCast.Models
{
    using Contracts;

    public class PromotionCriteriaModel : IPromotionCriteria
    {
        public decimal PromoterForClientPercentage { get; set; }
        public long MinCoinPrice { get; set; }
        public int MinFollowerCount { get; set; }

        public List<string> AllowedUsers { get; set; }

        public bool HasAllowedUsers() => AllowedUsers != null && AllowedUsers.Any(au => au.IsNotEmpty());

        public bool IsUserAllowedAsPromoter(IBitCloutUser user)
        {
            if (HasAllowedUsers())
                return AllowedUsers.Any(key => user.PublicKey.Equals(key));

            return (user.CoinPrice == null || user.CoinPrice > (ulong) MinCoinPrice) &&
                   (user.FollowerCount == null || user.FollowerCount > MinFollowerCount);
        }
    }

    public class PromotionCriteriaValidator : AbstractValidator<PromotionCriteriaModel>
    {
        public PromotionCriteriaValidator ()
        {
            RuleSet("create", () =>
            {
                RuleFor(pc => pc.MinCoinPrice).GreaterThanOrEqualTo(0);
                RuleFor(ph => ph.MinFollowerCount).GreaterThanOrEqualTo(0);
            });

            RuleSet("valid",() =>
            {
                RuleFor(pc => pc.MinCoinPrice).GreaterThan(0).Unless(pc => pc.MinFollowerCount > 0 || pc.HasAllowedUsers());
                RuleFor(pc => pc.MinFollowerCount).GreaterThan(0).Unless(pc => pc.MinCoinPrice > 0 || pc.HasAllowedUsers());
                RuleFor(pc => pc.AllowedUsers).NotEmpty().Unless(pc => pc.MinFollowerCount > 0 || pc.MinCoinPrice > 0);
            });
        }
    }
}