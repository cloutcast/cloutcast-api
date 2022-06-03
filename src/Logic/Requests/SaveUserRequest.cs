using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    using Entities;

    public class SaveUserRequest : ValidatedRequest<SaveUserRequest, BitCloutUser>
    {
        public IAppSource App { get; set; }
        public string Handle { get; set; }
        public string PublicKey { get; set; }

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(req => req.App).NotNull();
            v.RuleFor(req => req.Handle).NotEmpty();
            v
                .RuleFor(req => req.PublicKey)
                .NotEmpty().WithMessage("Must provide PublicKey")
                .DependentRules(() => v.RuleFor(request => request.PublicKey).MaximumLength(58));
        }
    }
}