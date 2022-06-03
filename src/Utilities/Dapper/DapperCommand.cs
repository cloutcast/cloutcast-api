using System.Threading;
using System.Threading.Tasks;
using FluentValidation;

namespace CloutCast
{
    public interface IDapperCommand : IDapperStatement { }

    public abstract class DapperCommand : DapperStatement, IDapperCommand { }

    public abstract class ValidatedDapperCommand<C> : DapperCommand, IValidated where C : ValidatedDapperCommand<C>
    {
        public void ValidateAndThrow(params string[] ruleSetNames) => GetValidator().Validate(
            (C) this,
            options =>
            {
                options.IncludeRuleSets(ruleSetNames);
                options.ThrowOnFailures();
            });

        public Task ValidateAndThrowAsync(CancellationToken cancellationToken, params string[] ruleSetNames) =>
            GetValidator().ValidateAsync(
                (C) this,
                options =>
                {
                    options.IncludeRuleSets(ruleSetNames);
                    options.ThrowOnFailures();
                },
                cancellationToken);


        private RequestValidator GetValidator()
        {
            var validator = new RequestValidator();
            SetupValidation(validator);
            return validator;
        }

        protected abstract void SetupValidation(RequestValidator v);

        protected class RequestValidator : AbstractValidator<C> { }
    }
}