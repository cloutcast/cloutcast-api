using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentValidation;

namespace CloutCast.Queries
{
    using Contracts;
    using Entities;
    using Models;
    using Records;

    public interface IGetMatchingPromotionsByQuery : IDapperQuery<List<Promotion>>, IValidated
    {
        IGetMatchingPromotionsByQuery Active(ActiveFlag flag);
        IGetMatchingPromotionsByQuery InboxUser(IBitCloutUser flag);
        IGetMatchingPromotionsByQuery ClientKey(string clientKey);
        IGetMatchingPromotionsByQuery IncludeEntityLogs(bool include);
        IGetMatchingPromotionsByQuery IncludePromoters(bool include);
        IGetMatchingPromotionsByQuery ForPromoter(IBitCloutUser promoter);
    }

    [JetBrains.Annotations.UsedImplicitly]
    public class GetMatchingPromotionsByQuery : ValidatedDapperQuery<GetMatchingPromotionsByQuery, List<Promotion>>,
        IGetMatchingPromotionsByQuery
    {
        public override void Build(IStatementBuilder builder)
        {
            SeedPromotionIds(builder);
            BuildPromotions(builder);
            BuildAllowedUsers(builder);
            BuildCriteria(builder);
            BuildEntityLog(builder);
            BuildPromoters(builder);
        }

        public override List<Promotion> Read(IDapperGridReader reader)
        {
            var promotions = ReadPromotions(reader);
            if (promotions == null || promotions.None()) return null;

            ReadAllowedUsers(reader, promotions);
            ReadCriteria(reader, promotions);
            ReadEntityLogs(reader, promotions);
            ReadPromoters(reader, promotions);

            return promotions;
        }

        #region internals
        private void BuildAllowedUsers(IStatementBuilder builder) => builder.Add($@"
select pu.PromotionId, pu.PublicKey, pu.ReadOn, u.BlackList as UserBlackList, u.Handle as UserHandle, u.Id as UserId
from {Tables.PromotionUsers} pu 
inner join @PromoIds pi on pu.PromotionId = pi.Id
left join {Tables.User} u on pu.PublicKey = u.PublicKey
");
        private void BuildCriteria(IStatementBuilder builder)
        {
            if (!HasPromoter()) return;

            builder.Add($@"
select p.UserId as ClientId, count(p.Id) as TotalPromotions, SUM(case when el.UserId is not null then 1 else 0 end) as TotalPromotionsDone
from {Tables.Promotion} p
inner join @PromoIds i on i.ClientId = p.UserId
left outer join {Tables.EntityLog} el on 
    el.PromotionId = p.Id and 
    el.Action = {(int) EntityAction.UserDidPromotion}
    and el.UserId = {_promoter.Id}
group by p.UserId");

/*
            builder.Add($@"

-- Total POW by Promoter by Client
select p.UserId as ClientId, el.UserId as PromoterId, count(p.Id) as TotalPromotionsDoneForClient
from {Tables.Promotion} p
inner join @PromoIds i on i.ClientId = p.UserId
inner join {Tables.EntityLog} el on el.PromotionId = p.Id and el.Action = {(int) EntityAction.UserDidPromotion}
group by p.UserId, el.UserId

-- Total Promotions By Client
select i.ClientId, count(i.ClientId) as TotalPromotions from @PromoIds i group by i.ClientId
");
*/
        }

        private void BuildEntityLog(IStatementBuilder builder)
        {
            if (_includeEntityLogs)
                builder.Add($@"
select el.Id, el.PromotionId, el.TimeStamp, el.Action, 
       bu.Id, bu.PublicKey, bu.Handle
from {Tables.EntityLog} el 
inner join {Tables.User} bu on el.UserId = bu.Id
inner join @PromoIds i on el.PromotionId = i.Id");
        }
        protected internal void BuildPromoters(IStatementBuilder builder)
        {
            // Get a list of all CREDITs to USER - PAYABLE
            if (_includePromoters)
                builder.Add($@"
select debit.PromotionId, promoter.Id, promoter.PublicKey, promoter.Handle
from {Tables.GeneralLedger} gl
inner join {Tables.GeneralLedgerAccount} debit on debit.Id = gl.DebitAccountId
inner join {Tables.GeneralLedgerAccount} credit on credit.Id = gl.CreditAccountId
inner join {Tables.User} promoter on promoter.Id = credit.UserId
inner join @PromoIds i on debit.PromotionId = i.Id
where credit.LedgerTypeId = {(int) GeneralLedgerType.Payable}
group by debit.PromotionId, promoter.Id, promoter.PublicKey, promoter.Handle");
        }
        
        protected internal void BuildPromotions(IStatementBuilder builder)
        {
            builder.Add($@"
select 
    p.Id, p.Duration, p.Engagements, p.SystemFee as Fee, p.Rate, p.MinCoinPrice, p.MinFollowerCount, p.BitCloutToUsdRate, p.TargetAction, p.TargetHex, p.TargetCreationDate, /* Header */
    bu.Id, bu.PublicKey, bu.Handle /* Client */ 
from  {Tables.Promotion} p
inner join {Tables.User} bu on p.UserId = bu.Id
inner join @PromoIds i on i.Id = p.Id

-- Get Client Profiles
select distinct up.UserId, up.Role, up.Setting, up.Value
from {Tables.UserProfile} up
inner join @PromoIds i on i.ClientId = up.UserId");
        }

        private void ReadAllowedUsers(IDapperGridReader reader, List<Promotion> promotions)
        {
            PromotionInboxModel ToInbox(PromotionAllowedUserRecord source)
            {
                var inbox = new PromotionInboxModel
                {
                    ReadOn = source.ReadOn,
                    User = new BitCloutUser
                    {
                        Id = source.UserId,
                        Handle = source.UserHandle,
                        PublicKey = source.PublicKey, 
                        BlackList = source.UserBlackList
                    }
                };
                return inbox;
            }

            var allowed = reader.Read<PromotionAllowedUserRecord>().ToList();
            var allowedUsersByPromotion = allowed
                .Where(r => r != null && 
                            r.PromotionId > 0 && 
                            r.PublicKey.IsNotEmpty())
                .GroupBy(r => r.PromotionId, ToInbox);

            foreach (var g in allowedUsersByPromotion)
            {
                var promotion = promotions.SingleOrDefault(p => p.Id == g.Key);
                if (promotion == null) continue;
                
                promotion.Criteria ??= new PromotionCriteriaModel();
                promotion.Criteria.AllowedUsers = g.Select(i => i.User.PublicKey).ToList();
                promotion.Inbox = g.ToList();
            }
        }
        private void ReadCriteria(IDapperGridReader reader, List<Promotion> promotions)
        {
            if (!HasPromoter()) return;

            var records = reader.Read<ClientPromoterPercentageRecord>().ToList();

            foreach (var promotion in promotions)
            {
                var record = records.SingleOrDefault(r => r.ClientId == promotion.Client.Id);
                if (record == null) continue;

                promotion.Criteria ??= new PromotionCriteriaModel();
                promotion.Criteria.PromoterForClientPercentage = record.Percentage;
            }
        }

        private void ReadEntityLogs(IDapperGridReader reader, List<Promotion> promotions)
        {
            if (!_includeEntityLogs) return;

            var logsByPromotion = reader
                .Map<EntityLogRecord, BitCloutUser>(Map.EntityLog)
                .GroupBy(r => r.PromotionId, r => new EntityLog(r));

            foreach (var g in logsByPromotion)
            {
                var promotion = promotions.SingleOrDefault(p => p.Id == g.Key);
                if (promotion == null) continue;
                promotion.Events = g.OrderBy(e => e.TimeStamp).ToList();
            }
        }
        
        private void ReadPromoters(IDapperGridReader reader, List<Promotion> promotions)
        {
            if (!_includePromoters) return;
            var promotersByPromotion =
                reader.Read<PromoterRecord>().GroupBy(pr => pr.PromotionId, pr => (BitCloutUser) pr);
            foreach (var g in promotersByPromotion)
            {
                var promotion = promotions.SingleOrDefault(p => p.Id == g.Key);
                if (promotion == null) continue;
                promotion.Promoters = g.Select(pu =>
                {
                    var bu = new BitCloutUser();
                    bu.CopyFrom(pu);
                    return bu;
                }).ToList();
            }
        }
        
        private List<Promotion> ReadPromotions(IDapperGridReader reader)
        {
            var promotions = reader
                .Read<PromotionHeaderRecord, BitCloutUser, Promotion>(Map.Promotion)
                .ToList();

            if (promotions.None()) return null;

            var profileGroups = reader.Read<UserSettingRecord>().GroupBy(r => r.UserId).ToList();
            foreach (var clientGrp in promotions.GroupBy(p => p.Client))
            {
                var client = clientGrp.Key;
                var settings = profileGroups
                    .Where(g => g.Key == client.Id)
                    .SelectMany(g => g.Select(r => r.ToSetting()));

                client.Profile ??= new UserProfile(settings);
                clientGrp.ForEach(p => p.Client = client);
            }

            return promotions;
        }

        private void SeedPromotionIds(IStatementBuilder builder)
        {
            builder
                .TableParam("PromoIds", "Id bigint, ClientId bigint")
                .Append($@"
insert into @PromoIds (Id, ClientId)
select source.PromotionId, p.UserId
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
	end) as CompletedEngagements
	from EntityLog el
	where el.TimeStamp <= GETUTCDATE()
	group by el.PromotionId
) source
inner join {Tables.Promotion} p on p.Id = source.PromotionId ");

            if (UseAllowedUser())
                builder
                    .Param("InboxUserPublicKey", _inboxUser.PublicKey)
                    .Append($@"
inner join {Tables.PromotionUsers} pu on pu.PromotionId = p.Id and pu.PublicKey = @InboxUserPublicKey");

            else if (UseClientKey())
                builder
                    .Param("ClientKey", _clientKey)
                    .Append($"inner join {Tables.User} bu on p.UserId = bu.Id and bu.PublicKey = @ClientKey");

            switch (_active)
            {
                case ActiveFlag.Both: return;

                case ActiveFlag.Active:
                    builder.Add("where source.Active > 0 and p.Engagements > source.CompletedEngagements");
                    break;

                case ActiveFlag.InActive:
                    builder.Add("where source.Active <= 0 or p.Engagements = source.CompletedEngagements");
                    break;
            }
        }

        private bool HasPromoter() => _promoter != null && _promoter.Id > 0;
        private bool UseAllowedUser() => _inboxUser != null && _inboxUser.Id > 0;
        private bool UseClientKey() => _clientKey != null && !_clientKey.IsEmpty();
        #endregion

        #region IGetPromotionsByQuery
        private ActiveFlag _active = ActiveFlag.Active;
        private IBitCloutUser _inboxUser;
        private IBitCloutUser _promoter;
        private string _clientKey;
        private bool _includeEntityLogs = true;
        private bool _includePromoters = true;

        public IGetMatchingPromotionsByQuery Active(ActiveFlag flag) => this.Fluent(x => _active = flag);
        public IGetMatchingPromotionsByQuery InboxUser(IBitCloutUser user) => this.Fluent(x => _inboxUser = user);
        public IGetMatchingPromotionsByQuery ClientKey(string clientKey) => this.Fluent(x => _clientKey = clientKey);
        public IGetMatchingPromotionsByQuery IncludeEntityLogs(bool include) => this.Fluent(x => _includeEntityLogs = include);
        public IGetMatchingPromotionsByQuery IncludePromoters(bool include) => this.Fluent(x => _includePromoters = include);
        public IGetMatchingPromotionsByQuery ForPromoter(IBitCloutUser promoter) => this.Fluent(x => _promoter = promoter);
        #endregion

        protected override void SetupValidation(RequestValidator v) => v
            .RuleFor(qry => qry)
            .Must(q =>
            {
                var cnt = 0;
                if (q.UseAllowedUser()) cnt += 1;
                if (q.UseClientKey()) cnt += 1;

                return cnt <= 1;
            })
            .WithMessage("Must set only one query parameter");

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"About to query {_active} promotions");
            if (UseAllowedUser()) 
                sb.Append($"; for Inbox User = {_inboxUser};");
            if (UseClientKey())
                sb.Append($"; for client; ClientKey = {_clientKey};");

            return sb.ToString();
        }
    }
}