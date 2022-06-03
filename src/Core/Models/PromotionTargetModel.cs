using System;
using FluentValidation;

namespace CloutCast.Models
{
    using Contracts;

    public class PromotionTargetModel : IPromotionTarget
    {
        public PromotionTargetModel() { }
        public PromotionTargetModel(IPromotionTarget source)
        {
            Action = source.Action;
            CreationDate = source.CreationDate;
            Hex = source.Hex;
        }

        public PromotionActivity Action { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public string Hex { get; set; }

        public M Clone<M>() where M : PromotionTargetModel, new() => new M
        {
            Action = Action,
            Hex = Hex
        };
    }

    public class PromotionTargetValidator : AbstractValidator<PromotionTargetModel>
    {
        public PromotionTargetValidator()
        {
            RuleFor(pt => pt.Action).Must(action => action != PromotionActivity.Undefined);
            RuleFor(pt => pt.Hex).NotEmpty().Length(64);
            RuleFor(pt => pt.CreationDate).NotNull().WithMessage("Must provide target post timestamp");
        }
    }
}