using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using log4net;
using PureLogicTek;

namespace SkunkWerx.CloutCast.Handlers
{
    using Commands;
    using Contracts;
    using Entities;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class MarkPromotionAsReadForUserHandler : ValidatedHandler<MarkPromotionAsReadForUserHandler, MarkPromotionAsReadForUserRequest, Promotion>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;
        
        public MarkPromotionAsReadForUserHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public override async Task<Promotion> Handle(MarkPromotionAsReadForUserRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var promotionId = request.PromotionId();

            // Mark as read
            var promotion = MarkAndGetById(promotionId, request.User());
            
            // check for any errors on the promotion
            await ValidatePromotion(promotion, cancellationToken);

            return promotion;
        }

        protected internal Promotion MarkAndGetById(long promotionId, IBitCloutUser user)
        {
            _log.Info($"Attempt to mark Promotion as read; PromotionId = {promotionId}; UserId={user.Id}");
            Promotion promotion = null;
            _pipeline
                .Command<MarkInboxAsReadCommand>(c =>
                {
                    c.PromotionId = promotionId;
                    c.User = user;
                })
                .Query<IGetPromotionByIdQuery, Promotion>(
                    q => q.PromotionId(promotionId).IncludeClientProfile(true),
                    p => promotion = p)
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();
            return promotion;
        }

        protected internal async Task ValidatePromotion(Promotion promotion, CancellationToken cancellationToken)
        {
            var validator = new HandlerValidator();
            validator.RuleFor(r => promotion).NotNull().WithMessage("Promotion not found");
            validator.RuleFor(r => promotion).Validate().IsActive();
            await Validate(validator, cancellationToken);
        }

        //Do not implement
        protected override void SetupValidation(HandlerValidator validator) { }
    }
}