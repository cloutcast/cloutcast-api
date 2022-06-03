using System.Linq;
using System.Text;
using FluentValidation;

namespace CloutCast.Queries
{
    using Contracts;

    public interface IGetMatchingPromotionsCountByQuery : IDapperQuery<long>, IValidated
    {
        ActiveFlag Active();
        IGetMatchingPromotionsCountByQuery Active(ActiveFlag flag);

        IBitCloutUser AllowedUser();
        IGetMatchingPromotionsCountByQuery AllowedUser(IBitCloutUser user);
    }

    [JetBrains.Annotations.UsedImplicitly]
    public class GetMatchingPromotionsCountByQuery : ValidatedDapperQuery<GetMatchingPromotionsCountByQuery, long>, IGetMatchingPromotionsCountByQuery
    {
        private ActiveFlag _active = ActiveFlag.Active;
        private IBitCloutUser _allowedUser;

        public override void Build(IStatementBuilder builder)
        {
            builder
                .Append($@"
select count(source.PromotionId)
from (
	select promotionId, 
	sum(case 
		when el.Action in ({(int) EntityAction.PromotionStart}, {(int) EntityAction.PromotionExtend}) then 1  
		when el.Action in ({(int) EntityAction.PromotionExpire}, {(int) EntityAction.PromotionStop}) then -1  
		else 0 
	end) as Active,
	sum(case 
		when el.Action = {(int) EntityAction.UserDidPromotion} then 1
		else 0
	end) as RemainingEngagement 
	from EntityLog el
	where el.TimeStamp <= GETUTCDATE()
	group by el.PromotionId
) source
inner join {Tables.Promotion} p on p.Id = source.PromotionId ");

            if (UseAllowedUser())
                builder
                    .Param("AllowedPublicKey", _allowedUser.PublicKey)
                    .Append($@"
inner join {Tables.PromotionUsers} pu on pu.PromotionId = p.Id 
and pu.PublicKey = @AllowedPublicKey 
and pu.ReadOn is null");

            switch (_active)
            {
                case ActiveFlag.Both: return;

                case ActiveFlag.Active:
                    builder.Add("where source.Active > 0 and p.Engagements > source.RemainingEngagement");
                    break;

                case ActiveFlag.InActive:
                    builder.Add("where source.Active <= 0 or p.Engagements = source.RemainingEngagement");
                    break;
            }
        }

        public override long Read(IDapperGridReader reader) => reader.Read<long>().SingleOrDefault();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"About to query {_active} promotions");
            if (UseAllowedUser()) 
                sb.Append($"; for Allowed User = {_allowedUser};");
            
            return sb.ToString();
        }

        protected override void SetupValidation(RequestValidator v) => v
            .RuleFor(qry => qry._allowedUser).NotNull().WithMessage("Must specify BitClout User");

        #region IGetMatchingPromotionsCountByQuery
        public ActiveFlag Active() => _active;
        public IBitCloutUser AllowedUser() => _allowedUser;
        protected internal bool UseAllowedUser() => _allowedUser != null && _allowedUser.PublicKey.IsNotEmpty();

        public IGetMatchingPromotionsCountByQuery Active(ActiveFlag flag) => this.Fluent(x => _active = flag);
        public IGetMatchingPromotionsCountByQuery AllowedUser(IBitCloutUser user) => this.Fluent(x => _allowedUser = user);
        #endregion
    }
}