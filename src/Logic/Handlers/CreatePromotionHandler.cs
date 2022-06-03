using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Contracts;
    using Commands;
    using Entities;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class CreatePromotionHandler : IRequestHandler<CreatePromotionRequest, Promotion>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public CreatePromotionHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        public async Task<Promotion> Handle(CreatePromotionRequest request, CancellationToken cancellationToken)
        {
            await request.ValidateAndThrowAsync(cancellationToken);

            var promotion = request.Promotion();
            promotion.Header.Fee = GenerateSystemFee(promotion.Header);
            WritePromotion(request.App(), request.Client(), promotion, DateTimeOffset.UtcNow, "NewPromotionId");

            return GetPromotionFromDb("NewPromotionId");
        }

        protected internal Promotion GetPromotionFromDb(string sqlParam)
        {
            Promotion result = null;
            _pipeline
                .Query<IGetPromotionByIdQuery, Promotion>(
                    q => q.PromotionIdParam(sqlParam),
                    p => result = p)
                .Run();
            return result;
        }

        protected internal long GenerateSystemFee(IPromotionHeader header) => 
            (long) Math.Ceiling(header.Rate * 1.12) - header.Rate;

        protected internal void WritePromotion(IAppSource app, IBitCloutUser client, Promotion source, DateTimeOffset startsOn, string sqlParam) => _pipeline
            .Command<ICheckAmountCommand>(c => c
                .Amount(source.TotalBudget())
                .AccountOwner(GeneralLedgerAccountType.User, client.Id)
                .ErrorMessage("Insufficient funds available to create promotion")
                .Ledger(GeneralLedgerType.Deposit))

            .Command<IPromotionCreateCommand>(c => c
                .Client(client)
                .OutputIdParam(sqlParam)
                .Promotion(source))

            .Command<IAppendToEntityLogCommand>(c => c
                .AsOf(startsOn, app)
                .OutputParam("PromoStartEventId")
                .Log(EntityAction.PromotionStart, client.Id, sqlParam))

            .Command<IAppendToEntityLogCommand>(c => c
                .AsOf(startsOn.AddMinutes(source.Header.Duration), app)
                .OutputParam("PromoExpireEventId")
                .Log(EntityAction.PromotionExpire, client.Id, sqlParam))

            .Command<IRecordGeneralLedgerCommand>(c => c
                .EntityLogParam("PromoStartEventId")
                .Debit(client.Id, GeneralLedgerAccountType.User, GeneralLedgerType.Deposit)
                .Credit(sqlParam, GeneralLedgerAccountType.Promotion, GeneralLedgerType.Payable)
                .Amount(source.TotalBudget())
                .Memo($"Move funds from {client} to start Promotion"))

            .Command<IReverseGeneralLedgerCommand>(r => r
                .SourceParam("PromoStartEventId")
                .TargetParam("PromoExpireEventId"))

            .UseIsolationLevel(IsolationLevel.Snapshot);
    }
}