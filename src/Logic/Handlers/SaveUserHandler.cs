using System.Data;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Commands;
    using Entities;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class SaveUserHandler: IRequestHandler<SaveUserRequest, BitCloutUser>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public SaveUserHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public async Task<BitCloutUser> Handle(SaveUserRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            _pipeline.Command<SaveUserCommand>(c => c.With(request.Handle, request.PublicKey, request.App.Id));
            
            return GetByPublicKey(request.PublicKey);
        }

        protected internal BitCloutUser GetByPublicKey(string publicKey)
        {
            BitCloutUser user = null;
            _pipeline
                .Query<GetBitCloutUserQuery, BitCloutUser>(
                    q => q.PublicKey(publicKey).IncludeProfile(false),
                    u => user = u)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();
            return user;
        }
    }
}