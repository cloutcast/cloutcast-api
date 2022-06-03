--and ca.PromotionId in (3, 5, 6, 13, 22, 23, 24, 25, 28, 29, 30, 31, 32, 34, 41, 49)

/*
-- Line 1
insert into GeneralLedger (Amount, EntityLogId, DebitAccountId, CreditAccountId, Memo)
select
	bad.OverSpent, 
	gl.EntityLogId, 
	3 as DebitAccountId,
	(
		select gla.id
		from GeneralLedgerAccount gla
		inner join (select p.UserId	from Promotion p where p.Id = bad.PromotionId) x  on x.UserId = gla.UserId 
		where gla.LedgerTypeId = 30
	) as CreditAccountId, 
	'Overage fix'
from GeneralLedger gl
inner join EntityLog el on el.Id = gl.EntityLogId
inner join GeneralLedgerAccount ca on ca.ID = gl.CreditAccountId
inner join OverSpent bad on bad.PromotionId = ca.PromotionId
where el.Action = 20
and ca.LedgerTypeId = 20
and gl.Amount = bad.FundedAmount
*/

/*
--Line 2 of Overage fix
insert into GeneralLedger (Amount, EntityLogId, DebitAccountId, CreditAccountId, Memo)
select bad.OverSpent, gl.EntityLogId, gl.DebitAccountId, gl.CreditAccountId, 'Overage fix'
from GeneralLedger gl
inner join EntityLog el on el.Id = gl.EntityLogId
inner join GeneralLedgerAccount ca on ca.ID = gl.CreditAccountId
inner join OverSpent bad on bad.PromotionId = ca.PromotionId
where el.Action = 20
and ca.LedgerTypeId = 20
and gl.Amount = bad.FundedAmount
*/



/*
--Reversal
insert into GeneralLedger (Amount, EntityLogId, DebitAccountId, CreditAccountId, Memo)
select bad.OverSpent, gl.EntityLogId, gl.DebitAccountId, gl.CreditAccountId, 'Overage fix'
from GeneralLedger gl
inner join EntityLog el on el.Id = gl.EntityLogId
inner join GeneralLedgerAccount da on da.ID = gl.DebitAccountId
inner join OverSpent bad on bad.PromotionId = da.PromotionId
where el.Action in (30,35)
and da.LedgerTypeId = 20
and gl.Amount = bad.FundedAmount
*/
