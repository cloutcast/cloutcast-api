using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;

    public class GetFollowerCountForUserRequest : ValidatedRequest<GetFollowerCountForUserRequest, long>
    {
        public IBitCloutUser User { get; set; }

        protected override void SetupValidation(RequestValidator v) => v
            .RuleFor(req => req.User).NotNull()
            .DependentRules(() =>
                v.RuleFor(req => req.User.PublicKey).NotEmpty().WithMessage("Missing User PublicKey"));

        public Body ToBody() => new Body {Username = User.Handle};

        public class Body
        {
            public string Username { get; set; }
            public string PublicKeyBase58Check => "";
            public bool GetEntriesFollowingUsername => true;
            public string LastPublicKeyBase58Check => "";
            public int NumToFetch => 1;
        }
    }
}