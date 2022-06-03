using FluentValidation;

namespace CloutCast.Contracts
{
    public interface IGeneralLedgerAccountOwner : IEntity
    {
        GeneralLedgerAccountType Type { get; }
        string Describe();
    }

    public static class GeneralLedgerAccountExtensions
    {
        public static string ToDistinctName(this IGeneralLedgerAccountOwner owner) => 
            owner== null ? "" : $"[{owner.Type} {owner.Id:00000}]";

        public static IRuleBuilderOptions<T, IGeneralLedgerAccountOwner> GLAccountOwner<T>(this IRuleBuilder<T, IGeneralLedgerAccountOwner> ruleBuilder) 
            => ruleBuilder.SetValidator(new GeneralLedgerAccountValidator());
    }

    public class GeneralLedgerAccountValidator : AbstractValidator<IGeneralLedgerAccountOwner>
    {
        public GeneralLedgerAccountValidator()
        {
            RuleFor(glo => glo.Id).Must(id => id > 0).WithMessage("AccountOwner Id must be greater than 0");
            RuleFor(glo => glo.Type)
                .Must(at => at != GeneralLedgerAccountType.Undefined)
                .WithMessage("Account Owner Type must be defined");
        }
    }

}