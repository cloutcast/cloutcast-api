using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using log4net;

namespace CloutCast.Handlers
{
    using Entities;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class GetPromotionHandler : ValidatedHandler<GetPromotionHandler, GetPromotionRequest, Promotion>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;
        private Promotion _result;
        private bool _throwIfNotActive;

        public GetPromotionHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public override async Task<Promotion> Handle(GetPromotionRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            _throwIfNotActive = request.ThrowIfNotActive();

            _result = GetById(request.PromotionId());

            await ValidateAndThrowAsync(cancellationToken);

            return _result;
        }

        protected override void SetupValidation(HandlerValidator validator)
        {
            validator.RuleFor(r => r._result).NotNull().WithMessage("Promotion not found");
            if (_throwIfNotActive)
                validator.RuleFor(r => r._result).Validate().IsActive();
            else
                validator.RuleFor(r => r._result).Validate();
        }

        protected internal Promotion GetById(long promotionId)
        {
            Promotion promotion = null;
            _pipeline
                .Query<IGetPromotionByIdQuery, Promotion>(
                    q => q.PromotionId(promotionId).IncludeClientProfile(true),
                    p => promotion = p)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();
            return promotion;
        }
    }
}