--reset  promotion to broken state
declare @PromoId as bigint
declare @Budget as bigint 
declare @Overage as bigint   
declare @SystemDepositAccount as bigint
declare @PromoPayableAccount as bigint

set @PromoId = 41
set @Budget  = 336896663
set @Overage = 375769357

select @SystemDepositAccount = gla.Id from GeneralLedgerAccount gla where gla.UserID = 1 and gla.LedgerTypeId = 30
select @PromoPayableAccount = gla.Id from GeneralLedgerAccount gla where gla.PromotionId = @PromoId and gla.LedgerTypeId = 20

/*
insert into GeneralLedger (Amount, EntityLogId, DebitAccountId, CreditAccountId, Memo)
select @Overage, el.Id, @SystemDepositAccount, @PromoPayableAccount, concat('Fix for over spending on promotion; PromotionId = ', @PromoId)
from GeneralLedger gl
inner join EntityLog el on el.Id = gl.EntityLogId
where el.PromotionId = @PromoId 
and el.Action in (20,30,35)
and gl.Amount > @Budget

update gl
set Amount = @Budget
from GeneralLedger gl
inner join EntityLog el on el.Id = gl.EntityLogId
where el.PromotionId = @PromoId
and Action in (20, 30, 35)
and gl.Amount > @Budget
and gl.DebitAccountId <> 3
*/

select el.Action,gl.*
from GeneralLedger gl
inner join EntityLog el on el.Id = gl.EntityLogId
where el.PromotionId = @PromoId
and Action in (20, 30, 35)
and (gl.Amount >= @Budget or gl.Amount = @Overage)


--update GeneralLedger set Amount = @Overage where id in (1823, 1824)