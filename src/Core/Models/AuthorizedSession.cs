using FluentValidation;

namespace CloutCast.Models
{
    using Contracts;
    using Entities;

    public class AuthorizedSession
    {
        public AppSource App { get; set; }
        public BitCloutUser User { get; set; }
    }

    public class AuthorizedSessionValidator : AbstractValidator<AuthorizedSession>
    {
        public AuthorizedSessionValidator()
        {
            RuleFor(session => session.User).NotNull().WithMessage("Missing Active User");
            RuleFor(session => session.User).BitCloutUser();
        }
    }
}