using System.Threading;
using System.Threading.Tasks;
using FluentValidation;

namespace CloutCast
{
    public interface IDapperQuery<R> : IDapperStatement 
    {
        R Read(IDapperGridReader reader);
    }

    public abstract class DapperQuery<R>: DapperStatement, IDapperQuery<R> 
    {
        public abstract R Read(IDapperGridReader reader);
    }

    public abstract class ValidatedDapperQuery<Q, R> : DapperQuery<R>, IValidated 
        where Q : ValidatedDapperQuery<Q, R>
    {
        public void ValidateAndThrow(params string[] ruleSetNames) =>
            GetValidator().Validate(
                (Q) this,
                options =>
                {
                    options.IncludeRuleSets(ruleSetNames);
                    options.ThrowOnFailures();
                });

        public Task ValidateAndThrowAsync(CancellationToken cancellationToken, params string[] ruleSetNames) =>
            GetValidator().ValidateAsync(
                (Q) this,
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

        protected class RequestValidator : AbstractValidator<Q> { }
    }
}