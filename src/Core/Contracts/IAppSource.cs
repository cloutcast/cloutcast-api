using FluentValidation;

namespace CloutCast.Contracts
{
    public interface IAppSource : IEntity
    {
        string ApiKey { get; }
        string Company { get; }
        string Name { get; }
    }

    public static class AppSourceExtensions
    {
        public static IRuleBuilderOptions<T, IAppSource> Validate<T>(this IRuleBuilder<T, IAppSource> ruleBuilder) 
            => ruleBuilder.SetValidator(new AppSourceValidator());
    }

    public class AppSourceValidator : AbstractValidator<IAppSource>
    {
        public AppSourceValidator()
        {
            RuleFor(user => user.Id).Must(id => id > 0).WithMessage("App Source Id must be greater than 0");
            RuleFor(user => user.ApiKey).NotEmpty().WithMessage("Missing ApiKey");
        }
    }

}