using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    using Entities;

    public class GetUserRequest : ValidatedRequest<GetUserRequest, BitCloutUser>
    {
        private bool _fetchFromBitClout = true;
        private bool _includeFollowerCount = true;
        private bool _includeProfile = true;
        private string _publicKey;
        private long _userId;

        public GetUserRequest FetchFromBitClout(bool shouldFetch) => this.Fluent(x => _fetchFromBitClout = shouldFetch);
        public bool FetchFromBitClout() => _fetchFromBitClout;

        /// <param name="includeFollowerCount"></param>
        /// <remarks>defaults true</remarks>
        public GetUserRequest IncludeFollowerCount(bool includeFollowerCount) => this.Fluent(x => _includeFollowerCount = includeFollowerCount);
        public bool IncludeFollowerCount() => _includeFollowerCount;

        public GetUserRequest IncludeProfile(bool includeProfile) => this.Fluent(x => _includeProfile = includeProfile);
        public bool IncludeProfile() => _includeProfile;

        public GetUserRequest PublicKey(string publicKey) => this.Fluent(x => _publicKey = publicKey);
        public string PublicKey() => _publicKey;
        
        private bool _throwOnNotFound = true;
        public bool ThrowOnNotFound() => _throwOnNotFound;
        public GetUserRequest ThrowOnNotFound(bool throwOnNotFound) => this.Fluent(x => _throwOnNotFound = throwOnNotFound);

        public GetUserRequest UserId(long userId) => this.Fluent(x => _userId = userId);
        public long UserId() => _userId;

        public GetUserRequest User(IBitCloutUser user)
        {
            _userId = user?.Id ?? 0;
            _publicKey = user?.PublicKey;

            return this;
        } 

        protected override void SetupValidation(RequestValidator v) => v
            .When(
                request => request._publicKey.IsEmpty(),
                () => v.RuleFor(r => r._userId).GreaterThan(0)
            )
            .Otherwise(() => v
                .RuleFor(request => request._publicKey)
                .NotEmpty().WithMessage("Must provide PublicKey")
                .DependentRules(() => v.RuleFor(request => request._publicKey).MaximumLength(58))
            );
    }
}