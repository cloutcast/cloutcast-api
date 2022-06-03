using System;
using System.Linq;
using FluentValidation;

namespace CloutCast.Queries
{
    using Commands;
    using Models;

    public interface IGetBalanceForPromotionQuery : IDapperQuery<AccountBalanceModel>
    {
        IGetBalanceForPromotionQuery AsOf(DateTimeOffset? asOf);
        IGetBalanceForPromotionQuery PromotionId(long promotionId);
    }

    public class GetBalanceForPromotionQuery : ValidatedDapperQuery<GetBalanceForPromotionQuery, AccountBalanceModel>, IGetBalanceForPromotionQuery
    {
        private readonly IGeneralLedgerBalanceCommand _promoCredits;
        private readonly IGeneralLedgerBalanceCommand _promoDebits;

        public GetBalanceForPromotionQuery(IGeneralLedgerBalanceCommand promoCredits, IGeneralLedgerBalanceCommand promoDebits)
        {
            _promoCredits = promoCredits;
            _promoDebits = promoDebits;
        }

        #region IGetPromotionBalanceQuery
        private DateTimeOffset? _asOf;
        public IGetBalanceForPromotionQuery AsOf(DateTimeOffset? asOf) => this.Fluent(x => _asOf = asOf);
        
        private long _promotionId;
        public IGetBalanceForPromotionQuery PromotionId(long promotionId) => this.Fluent(x => _promotionId = promotionId);
        #endregion

        public override void Build(IStatementBuilder builder)
        {
            _promoCredits
                .Action(GeneralLedgerAction.Credit)
                .Account(GeneralLedgerAccountType.Promotion, _promotionId)
                .Ledger(GeneralLedgerType.Payable)
                .BalanceParam("PromoCredits");

            _promoDebits
                .Action(GeneralLedgerAction.Debit)
                .Account(GeneralLedgerAccountType.Promotion, _promotionId)
                .BalanceParam("PromoDebits")
                .Ledger(GeneralLedgerType.Payable);

            if (_asOf != null)
            {
                _promoCredits.AsOf(_asOf.Value);
                _promoDebits.AsOf(_asOf.Value);
            }

            _promoCredits.Build(builder);
            _promoDebits.Build(builder);

            builder
                .Add("select @PromoCredits")
                .Add("select @PromoDebits");
        }

        public override AccountBalanceModel Read(IDapperGridReader reader)
        {
            var credits = reader.Read<long>().SingleOrDefault();
            var debits = reader.Read<long>().SingleOrDefault();

            return new AccountBalanceModel
            {
                AccountOwner = new GLAccountOwnerModel
                {
                    Id = _promotionId,
                    Type = GeneralLedgerAccountType.Promotion
                },
                AsOf = _asOf ?? DateTimeOffset.UtcNow,
                Settled = credits - debits,
                UnSettled = debits
            };
        }
        
        protected override void SetupValidation(RequestValidator v) => v.RuleFor(q => q._promotionId).GreaterThan(0);
    }
}