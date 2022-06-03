using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;

namespace CloutCast
{
    public abstract class ValidatedHandler<H, R> : IRequestHandler<R>, IValidated
        where H : ValidatedHandler<H, R>
        where R : IRequest
    {
        public abstract Task<Unit> Handle(R request, CancellationToken cancellationToken);

        protected abstract void SetupValidation(HandlerValidator validator);

        #region IValidated
        protected class HandlerValidator : AbstractValidator<H> { }
        
        public Task ValidateAndThrowAsync(CancellationToken cancellationToken, params string[] ruleSetNames)
        {
            var validator = new HandlerValidator();
            SetupValidation(validator);

            return validator.ValidateAsync((H) this, options =>
            {
                options.IncludeRuleSets(ruleSetNames);
                options.ThrowOnFailures();
            }, cancellationToken);
        }

        public void ValidateAndThrow(params string[] ruleSetNames)
        {
            var validator = new HandlerValidator();
            SetupValidation(validator);
            validator.Validate((H) this, options =>
            {
                options.IncludeRuleSets(ruleSetNames);
                options.ThrowOnFailures();
            });
        }
        #endregion
    }
    
    public abstract class ValidatedHandler<H, R, T> : IRequestHandler<R, T>, IValidated
        where H : ValidatedHandler<H, R, T>
        where R : IRequest<T>
    {
        public abstract Task<T> Handle(R request, CancellationToken cancellationToken);

        protected abstract void SetupValidation(HandlerValidator validator);

        #region IValidated

        protected class HandlerValidator : AbstractValidator<H> { }

        protected Task Validate(HandlerValidator validator, CancellationToken cancellationToken, params string[] ruleSetNames)
        {
            return validator.ValidateAsync(
                (H) this,
                options =>
                {
                    options.IncludeRuleSets(ruleSetNames);
                    options.ThrowOnFailures();
                },
                cancellationToken);
        }
        
        public Task ValidateAndThrowAsync(CancellationToken cancellationToken, params string[] ruleSetNames)
        {
            var validator = new HandlerValidator();
            SetupValidation(validator);
            return Validate(validator, cancellationToken, ruleSetNames);
        }

        public void ValidateAndThrow(params string[] ruleSetNames)
        {
            var validator = new HandlerValidator();
            SetupValidation(validator);

            validator.Validate(
                (H) this,
                options =>
                {
                    options.IncludeRuleSets(ruleSetNames);
                    options.ThrowOnFailures();
                });
        }

        #endregion
    }
}