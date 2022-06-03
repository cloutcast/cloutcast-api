using System;
using FluentValidation;

namespace CloutCast.Commands
{
    using Entities;

    public interface IRefundUserWorkCommand : IDapperCommand, IValidated
    {
        IRefundUserWorkCommand AsOf(DateTimeOffset asOf);
        IRefundUserWorkCommand ProofOfWorkLedger(GeneralLedgerItem ledger);
    }

    public class RefundUserWorkCommand : ValidatedDapperCommand<RefundUserWorkCommand>, IRefundUserWorkCommand
    {
        private DateTimeOffset _asOf;
        private GeneralLedgerItem _powLedger;

        #region IRefundUserWorkCommand
        public IRefundUserWorkCommand AsOf(DateTimeOffset asOf) => this.Fluent(x => _asOf = asOf);
        public IRefundUserWorkCommand ProofOfWorkLedger(GeneralLedgerItem ledger) => this.Fluent(x => _powLedger = ledger);
        #endregion

        public override void Build(IStatementBuilder builder)
        {
            var powId = _powLedger.EntityLog.Id;
            var refundVal = (int) EntityAction.UserWorkRefund;

            builder
                .Shared("AsOf", _asOf)
                .Param("AppId", 0L).Add($"select @AppId = Id from {Tables.App} where Name = 'System'")

                .TableParam("Refunds", "PowId bigint, RefundEventId bigint")
                .Add($@"
INSERT INTO {Tables.EntityLog} (TimeStamp, Action, AppId, UserId, PromotionId)
OUTPUT {powId} as PowId, Inserted.Id as RefundEventId INTO @Refunds
SELECT @AsOf, {refundVal}, @AppId, didPromoEl.UserId, didPromoEl.PromotionId
FROM {Tables.EntityLog} didPromoEl
WHERE didPromoEl.Id = {powId} 
AND didPromoEl.Action = {(int) EntityAction.UserDidPromotion}
AND NOT EXISTS ( 
	select id 
	from EntityLog el 
	where el.UserId = didPromoEl.UserId 
	and el.PromotionId = didPromoEl.PromotionId 
	and el.Action = {refundVal}
)")

                .Add($@"
INSERT INTO {Tables.GeneralLedger} (Amount, EntityLogId, DebitAccountId, CreditAccountId, Memo)
SELECT {_powLedger.Amount}, r.RefundEventId, payable.Id, deposit.Id, 
       Concat('Refund Client ', client.Handle, ' from user ', badPromoter.Handle)
FROM {Tables.GeneralLedger} gl
INNER JOIN @Refunds r on r.PowId = gl.EntityLogId
INNER JOIN {Tables.EntityLog} el on el.Id = gl.EntityLogId

INNER JOIN {Tables.Promotion} promo on promo.Id = el.PromotionId
INNER JOIN {Tables.User} client on client.Id = promo.UserId

INNER JOIN {Tables.GeneralLedgerAccount} payable on (payable.LedgerTypeId = {(int) GeneralLedgerType.Payable} and payable.Id = gl.CreditAccountId)  --Promoter.Payable
INNER JOIN {Tables.GeneralLedgerAccount} deposit on (deposit.LedgerTypeId = {(int) GeneralLedgerType.Deposit} and deposit.UserId = client.Id) -- Client.Deposit
INNER JOIN {Tables.User} badPromoter on badPromoter.Id = payable.UserId
WHERE r.PowId = {powId}")

                .Add($@"
UPDATE vw 
SET vw.Result = 0
FROM {Tables.ValidateWork} vw 
WHERE vw.EntityLogId = {powId}
AND EXISTS (
    select RefundEventId 
    from @Refunds 
    where PowId = {powId}
)");
        }

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(cmd => cmd._powLedger).NotNull();
            v.RuleFor(cmd => cmd._powLedger.EntityLog).NotNull();
            v.RuleFor(cmd => cmd._powLedger.EntityLog.Action).Equal(EntityAction.UserDidPromotion);
            v.RuleFor(cmd => cmd._asOf).GreaterThan(_powLedger.EntityLog.TimeStamp);
        }

    }
}