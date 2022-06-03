using System;
using FluentValidation;
using JetBrains.Annotations;

namespace CloutCast.Commands
{
    using Entities;
    
    public interface IPayoutUserWorkCommand : IDapperCommand, IValidated
    {
        IPayoutUserWorkCommand AsOf(DateTimeOffset asOf);
        IPayoutUserWorkCommand ProofOfWorkLedger(GeneralLedgerItem ledger);
    }

    [UsedImplicitly]
    public class PayoutUserWorkCommand : ValidatedDapperCommand<PayoutUserWorkCommand>, IPayoutUserWorkCommand
    {
        private DateTimeOffset _asOf;
        private GeneralLedgerItem _powLedger;

        #region IPayoutUserWorkCommand
        public IPayoutUserWorkCommand AsOf(DateTimeOffset asOf) => this.Fluent(x => _asOf = asOf);
        public IPayoutUserWorkCommand ProofOfWorkLedger(GeneralLedgerItem ledger) => this.Fluent(x => _powLedger = ledger);
        #endregion

        public override void Build(IStatementBuilder builder)
        {
            var didWorkId = _powLedger.EntityLog.Id;
            var payOutVal = (int) EntityAction.UserWorkPayOut;
            
            builder
                .Shared("AsOf", _asOf)
                .TableParam("Payouts", "PowId bigint, PayOutId bigint")
                
                .Add($@"
INSERT INTO {Tables.EntityLog} (TimeStamp, Action, AppId, UserId, PromotionId)
OUTPUT {didWorkId} as PowId, Inserted.Id as PayoutId INTO @Payouts
SELECT @AsOf, {payOutVal}, didPromoEl.AppId, didPromoEl.UserId, didPromoEl.PromotionId
FROM {Tables.EntityLog} didPromoEl
WHERE didPromoEl.Id = {didWorkId} 
AND NOT EXISTS ( 
	select id 
	from EntityLog el 
	where el.UserId = didPromoEl.UserId 
	and el.PromotionId = didPromoEl.PromotionId 
	and el.Action = {payOutVal}
)")

                .Add($@"
INSERT INTO {Tables.GeneralLedger} (Amount, EntityLogId, DebitAccountId, CreditAccountId, Memo)
SELECT {_powLedger.Amount}, p.PayOutId, payable.Id, deposit.Id, 
       Concat('Confirm Payout from Promotion[', el.PromotionId, '] for ', promoter.Handle)
FROM {Tables.GeneralLedger} gl
INNER JOIN @Payouts p on p.PowId = gl.EntityLogId
INNER JOIN {Tables.EntityLog} el on el.Id = gl.EntityLogId

INNER JOIN {Tables.User} promoter on promoter.Id = el.UserId

INNER JOIN {Tables.GeneralLedgerAccount} payable on (payable.LedgerTypeId = {(int) GeneralLedgerType.Payable} and payable.Id = gl.CreditAccountId) --Promoter.Payable
INNER JOIN {Tables.GeneralLedgerAccount} deposit on (deposit.LedgerTypeId = {(int) GeneralLedgerType.Deposit} and deposit.UserId = payable.UserId) -- Promoter.Deposit
WHERE p.PowId = {didWorkId}")
                
                .Add($@"
UPDATE vw 
SET vw.Result = 1
FROM {Tables.ValidateWork} vw 
WHERE vw.EntityLogId = {didWorkId}
AND EXISTS (
    select PayOutId 
    from @Payouts 
    where PowId = {didWorkId}
)");
        }

        protected override void SetupValidation(RequestValidator v)
        {
            var pow = _powLedger.EntityLog;

            v.RuleFor(cmd => cmd._powLedger).NotNull();
            v.RuleFor(cmd => cmd._powLedger.EntityLog).NotNull();
            v.RuleFor(cmd => cmd._powLedger.EntityLog.Action).Equal(EntityAction.UserDidPromotion);
            v.RuleFor(cmd => cmd._asOf).GreaterThan(pow.TimeStamp);
        }

    }
}