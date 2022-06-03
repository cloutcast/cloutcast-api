select 
    Engagements.PromotionId,
	Greatest(Engagements.Count, SystemFee.ActualEngagements) as TrueEngagementCount,
	SystemFee.AverageFee

from
(
	select x.PromotionId, NoFeeBudget/ (Rate* 1000000) as Count
	from (
		select p.Id as PromotionId, p.Rate, Cast( ( (p.Budget / 1.04 ) * 1000000) as  bigint) as NoFeeBudget		
		from Promotion p
	) x
) Engagements
inner join (
	select el.PromotionId, Avg(gl.Amount) as AverageFee, count(*) as ActualEngagements
	from GeneralLedger gl
	inner join EntityLog el on el.ID = gl.EntityLogId
	where el.Action = 17
	group by el.PromotionId
) as SystemFee on Engagements.PromotionId = SystemFee.PromotionId
order by PromotionId

--23 overspent