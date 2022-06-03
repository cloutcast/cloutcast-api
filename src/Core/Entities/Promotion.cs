using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;

namespace CloutCast.Entities
{
    using Models;

    public class Promotion : AbstractEntity<Promotion>
    {
        public BitCloutUser Client { get; set; }
        public PromotionContextModel Context { get; set; }
        public PromotionHeaderModel Header { get; set; }
        public List<PromotionInboxModel> Inbox { get; set; }
        public PromotionCriteriaModel Criteria { get; set; }
        public List<EntityLog> Events { get; set; }
        public List<BitCloutUser> Promoters { get; set; }
        public PromotionTargetModel Target { get; set; }

        public long Budget() => Header.Rate + Header.Fee;
        public long TotalBudget() => Budget() * Header.Engagements;
    }

    internal class PromotionValidator : AbstractValidator<Promotion>
    {
        internal PromotionValidator()
        {
            RuleFor(promotion => promotion.Id).Must(id => id > 0).WithMessage("Id must be greater than 0");
            RuleSet("isPromotionActive", () =>
            {
                RuleFor(r => r.Events)
                    .Custom((source, context) =>
                    {
                        var el = source
                            .Where(e => e.TimeStamp <= DateTimeOffset.UtcNow)
                            .OrderBy(e => e.TimeStamp)
                            .ToList();

                        var endEvent = el.Stopped() ?? el.Expired();
                        if (endEvent == null) return;
                        context.AddFailure(
                            $"Promotion already {(endEvent.Action == EntityAction.PromotionExpire ? "expired" : "stopped")} on {endEvent.TimeStamp:G}");
                    });
            });
        }
    }

    public static class PromotionDomainSpecificLanguageExtensions
    {
        public static bool IsActiveOn(this IEnumerable<EntityLog> source, DateTimeOffset when) => source
            .Where(e => e.TimeStamp <= when)
            .OrderBy(e => e.TimeStamp)
            .All(log => log.Action != EntityAction.PromotionStop &&
                        log.Action != EntityAction.PromotionExpire);

        public static EntityLog Extended(this IEnumerable<EntityLog> source) => source.LastOrDefault(e => e.Action == EntityAction.PromotionExtend);
        public static EntityLog Expired(this IEnumerable<EntityLog> source) => source.LastOrDefault(e => e.Action == EntityAction.PromotionExpire);
        public static EntityLog Started(this IEnumerable<EntityLog> source) => source.FirstOrDefault(e => e.Action == EntityAction.PromotionStart);
        public static EntityLog Stopped(this IEnumerable<EntityLog> source) => source.LastOrDefault(e => e.Action == EntityAction.PromotionStop);
    }

    public static class PromotionValidationExtensions
    {
        public static IRuleBuilderOptions<T, Promotion> IsActive<T>(this IRuleBuilder<T, Promotion> ruleBuilder) 
            => ruleBuilder.SetValidator(new PromotionValidator(), "isPromotionActive");
        public static IRuleBuilderOptions<T, Promotion> Validate<T>(this IRuleBuilder<T, Promotion> ruleBuilder) 
            => ruleBuilder.SetValidator(new PromotionValidator());
    }
}