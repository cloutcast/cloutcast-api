select p.Id, p.UserId, funded.PromoFunded, spent.PromoSpent, (funded.PromoFunded - spent.PromoSpent) as OverSpent
from (
	select el.PromotionId, sum(gl.amount) as PromoFunded
	from GeneralLedger  gl
	inner join EntityLog el on el.Id = gl.EntityLogId
	where el.Action = 20
	group by el.PromotionId
) funded
left outer join (
	select el.PromotionId, sum(gl.amount) PromoSpent
	from GeneralLedger  gl
	inner join EntityLog el on el.Id = gl.EntityLogId
	where el.Action = 15
	group by el.PromotionId
) spent on spent.PromotionId = funded.PromotionId
inner join Promotion p on p.Id = funded.PromotionId
where spent.PromoSpent > funded.PromoFunded