using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Entities;
    using Models;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class CreateAuthorizedSessionHandler : IRequestHandler<CreateAuthorizedSessionRequest, AuthorizedSession>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly CreateAuthorizedSessionValidator _requestValidator;
        private readonly AuthorizedSessionValidator _sessionValidator;
        private readonly ILog _log;

        public CreateAuthorizedSessionHandler(
            IDapperPipeline pipeline, 
            CreateAuthorizedSessionValidator validator, 
            AuthorizedSessionValidator sessionValidator,
            ILog log)
        {
            _pipeline = pipeline;
            _requestValidator = validator;
            _sessionValidator = sessionValidator;
            _log = log;
        }

        public async Task<AuthorizedSession> Handle(CreateAuthorizedSessionRequest request, CancellationToken cancellationToken)
        {
            await _requestValidator.ValidateAndThrowAsync(request, cancellationToken);

            var session = GetAuthorizedSession(request.UserPublicKey);
            await _sessionValidator.ValidateAndThrowAsync(session, cancellationToken);
            
            return session;
        }

        protected internal AuthorizedSession GetAuthorizedSession(string publicKey)
        {
            var session = new AuthorizedSession();
            _pipeline
                .Query<IGetBitCloutUserQuery, BitCloutUser>(
                    q => q.PublicKey(publicKey),
                    u => session.User = u)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return session;
        }
    }
}