select 
    z.PromoId,
	Greatest(z.EngagementCount, plogs.ActualEngagements) as TrueEngagementCount,
	plogs.TotalSystemFee / plogs.ActualEngagements as RealSystemFee,
	plogs.AverageFee,
	(plogs.TotalSystemFee / plogs.ActualEngagements) - plogs.AverageFee as Checker

from
(
	select 
		x.*,    NoFeeBudget/ (Rate* 1000000) as EngagementCount

	from (
		select 
			p.Id as PromoId,
			Cast( ( (p.Budget / 1.04 ) * 1000000) as  bigint) as NoFeeBudget, 
			p.Budget, 
			p.Rate
		from Promotion p
	) x
) z
inner join (
	select Sum(gl.Amount) as TotalSystemFee, Avg(gl.Amount) as AverageFee,  el.PromotionId, count(*) as ActualEngagements
	from GeneralLedger gl
	inner join EntityLog el on el.ID = gl.EntityLogId
	where el.Action = 17
	group by el.PromotionId
) as plogs on z.PromoId = plogs.PromotionId
order by PromoId

--23 overspent