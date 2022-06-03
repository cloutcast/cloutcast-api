using System.Linq;
using FluentValidation;

namespace CloutCast.Commands
{
    using Contracts;
    using Entities;
    using Models;

    public interface IPromotionCreateCommand: IDapperCommand, IValidated
    {
        IPromotionCreateCommand Client(IBitCloutUser client);
        IPromotionCreateCommand OutputIdParam(string paramName);
        void Promotion(Promotion source);
    }

    [JetBrains.Annotations.UsedImplicitly]
    public class PromotionCreateCommand : ValidatedDapperCommand<PromotionCreateCommand>, IPromotionCreateCommand
    {
        private IBitCloutUser _client;
        private string _outputParamName = "";
        private Promotion _source;

        #region IPromotionCreateCommand
        public IPromotionCreateCommand Client(IBitCloutUser client) => this.Fluent(x => _client = client);
        public IPromotionCreateCommand OutputIdParam(string paramName) => this.Fluent(x => _outputParamName = paramName);
        public void Promotion(Promotion source) => this.Fluent(x => _source = source);
        #endregion

        #region Statements
        private void SetupGeneralLedgerAccounts(IStatementBuilder builder) => builder.Add($@"
INSERT INTO {Tables.GeneralLedgerAccount} (LedgerTypeId, PromotionId)
VALUES 
  ({(int) GeneralLedgerType.Deposit}, @{_outputParamName}),
  ({(int) GeneralLedgerType.Payable}, @{_outputParamName})");

        private void WritePromotion(IStatementBuilder builder)
        {
            var criteria = _source.Criteria ?? new PromotionCriteriaModel();
            var header = _source.Header;
            var target = _source.Target;
            
            builder
                .Param("TargetCreationDate", target.CreationDate)
                .Param("TargetHex", target.Hex)
                .Param("ClientKey", _client.PublicKey)
                
                .TableParam("PromoIds", "Id bigint")
                .Add($@"
INSERT INTO Promotion (UserId, Duration, Engagements, BitCloutToUsdRate, Rate, SystemFee, MinCoinPrice, MinFollowerCount, TargetAction, TargetHex, TargetCreationDate)
OUTPUT inserted.Id as Id into @PromoIds
SELECT bcu.Id, {header.Duration}, {header.Engagements}, {header.BitCloutToUsdRate}, {header.Rate}, {header.Fee}, {criteria.MinCoinPrice}, {criteria.MinFollowerCount}, {(int) target.Action}, @TargetHex, @TargetCreationDate
FROM {Tables.User} bcu
WHERE bcu.PublicKey = @ClientKey")
                .Add($@"SELECT @{_outputParamName} = Id FROM @PromoIds");
            
            if (!criteria.HasAllowedUsers()) return;
            builder
                .Table("AllowedUsers", "UT_PromotionUsers", criteria.AllowedUsers.Where(au => au.IsNotEmpty()), m => m
                    .Map("PromotionId", au => 0)
                    .String("PublicKey", 58, au => au))
                .Append(@"
insert into PromotionUsers (PromotionId, PublicKey)
select p.Id, au.PublicKey 
from @AllowedUsers au, @PromoIds p");
        }
        #endregion

        public override void Build(IStatementBuilder builder)
        {
            if (!_outputParamName.IsEmpty())
                builder.Shared(_outputParamName, 0L);
            else
            {
                _outputParamName = "NewPromotionId";
                builder.Param(_outputParamName, 0L);
            }

            WritePromotion(builder);
            SetupGeneralLedgerAccounts(builder);
        }

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(c => c._client).NotNull().BitCloutUser();
            v.RuleFor(c => c._source).NotNull();
        }
    }
}