using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;

    public class GetPostByHashHexRequest : ValidatedRequest<GetPostByHashHexRequest, IBitCloutPost>
    {
        private readonly Body _body = new Body();
        
        public GetPostByHashHexRequest Comment(int offset, int limit)
        {
            _body.CommentOffset = offset;
            _body.CommentLimit = limit;
            return this;
        }
        public GetPostByHashHexRequest FetchParents(bool fetchParents) => this.Fluent(x => _body.FetchParents = fetchParents);
        public GetPostByHashHexRequest PostHashHex(string postHex, bool throwOnMissing = true)
        {
            ThrowOnMissingPost = throwOnMissing;
            return this.Fluent(x => _body.PostHashHex = postHex);
        }
        public GetPostByHashHexRequest ReaderPublicKeyBase58Check(string readerPublicKey) => this.Fluent(x => _body.ReaderPublicKeyBase58Check = readerPublicKey);
    
        public bool ThrowOnMissingPost { get; private set; } = true;

        public Body ToBody() => _body;

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(r => r._body.PostHashHex).NotEmpty();
        }
        
        public class Body
        {
            public string PostHashHex { get; set; }
            public string ReaderPublicKeyBase58Check { get; set; }
            public bool FetchParents { get; set; }
            public int CommentOffset { get; set; }
            public int CommentLimit { get; set; }
            public bool AddGlobalFeedBool { get; set; }
        }
    }
}