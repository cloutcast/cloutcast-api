using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;

namespace CloutCast
{
    public abstract class ValidatedRequest<R> : IRequest, IValidated where R : ValidatedRequest<R>
    {
        public Task ValidateAndThrowAsync(CancellationToken cancellationToken, params string[] ruleSetNames) =>
            GetValidator().ValidateAsync(
                (R) this,
                options =>
                {
                    options.IncludeRuleSets(ruleSetNames);
                    options.ThrowOnFailures();
                },
                cancellationToken);

        public void ValidateAndThrow(params string[] ruleSetNames) => GetValidator().Validate(
            (R) this,
            options =>
            {
                options.IncludeRuleSets(ruleSetNames);
                options.ThrowOnFailures();
            });

        private RequestValidator GetValidator()
        {
            var validator = new RequestValidator();
            SetupValidation(validator);
            return validator;
        }

        protected abstract void SetupValidation(RequestValidator validator);

        protected class RequestValidator : AbstractValidator<R> { }
    }

    public abstract class ValidatedRequest<R, T> : IRequest<T>, IValidated where R : ValidatedRequest<R, T>
    {
        public Task ValidateAndThrowAsync(CancellationToken cancellationToken, params string[] ruleSetNames) =>
            GetValidator().ValidateAsync(
                (R) this,
                options =>
                {
                    options.IncludeRuleSets(ruleSetNames);
                    options.ThrowOnFailures();
                },
                cancellationToken);

        public void ValidateAndThrow(params string[] ruleSetNames) => GetValidator().Validate(
            (R) this,
            options =>
            {
                options.IncludeRuleSets(ruleSetNames);
                options.ThrowOnFailures();
            });

        private RequestValidator GetValidator()
        {
            var validator = new RequestValidator();
            SetupValidation(validator);
            return validator;
        }

        protected abstract void SetupValidation(RequestValidator validator);

        protected class RequestValidator : AbstractValidator<R> { }
    }
}