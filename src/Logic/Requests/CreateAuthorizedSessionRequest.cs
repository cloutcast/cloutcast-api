using FluentValidation;
using MediatR;

namespace CloutCast.Requests
{
    using Models;

    public class CreateAuthorizedSessionRequest : IRequest<AuthorizedSession>
    {
        public string UserPublicKey { get; set; }
    }

    public class CreateAuthorizedSessionValidator : AbstractValidator<CreateAuthorizedSessionRequest>
    {
        public CreateAuthorizedSessionValidator()
        {
            RuleFor(request => request.UserPublicKey).NotNull().WithMessage("Missing User PublicKey");
        }
    }
}