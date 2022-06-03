using System.Linq;

namespace CloutCast.Queries
{
    using Entities;
    using Models;
    using Records;

    public interface IGetPromotionByIdQuery : IDapperQuery<Promotion>
    {
        IGetPromotionByIdQuery IncludeClientProfile(bool include);
        IGetPromotionByIdQuery PromotionId(long promotionId);
        IGetPromotionByIdQuery PromotionIdParam(string idParam);
    }

    [JetBrains.Annotations.UsedImplicitly]
    public class GetPromotionByIdQuery : DapperQuery<Promotion>, IGetPromotionByIdQuery
    {
        private bool _includeClientProfile = false;
        private long _promotionId;
        private string _promotionIdParam;

        #region IGetPromotionByIdQuery
        public IGetPromotionByIdQuery IncludeClientProfile(bool include) => this.Fluent(x => _includeClientProfile = include);
        public IGetPromotionByIdQuery PromotionId(long promotionId) => this.Fluent(x => _promotionId = promotionId);
        public IGetPromotionByIdQuery PromotionIdParam(string idParam) => this.Fluent(x => _promotionIdParam = idParam);
        #endregion

        public override void Build(IStatementBuilder builder)
        {
            var idVal = _promotionId > 0 ? $"{_promotionId}" : $"@{_promotionIdParam}";

            builder.Add($@"
select 
    p.Id, p.Duration, p.Engagements, p.SystemFee as Fee, p.Rate, p.MinCoinPrice, p.MinFollowerCount, p.BitCloutToUsdRate, p.TargetAction, p.TargetHex, p.TargetCreationDate , /* Header */
    bu.Id, bu.PublicKey, bu.Handle /* Client */ 
from  {Tables.Promotion} p
inner join {Tables.User} bu on p.UserId = bu.Id
where p.Id = {idVal}")

                .Add($@"
select el.Id, el.TimeStamp, el.Action, 
       bu.Id, bu.PublicKey, bu.Handle
from {Tables.EntityLog} el 
inner join {Tables.User} bu on el.UserId = bu.Id
where el.PromotionId = {idVal}")

                //Inbox
                .Add($@"
select pu.PromotionId, pu.PublicKey, pu.ReadOn, u.BlackList as UserBlackList, u.Handle as UserHandle, u.Id as UserId
from {Tables.PromotionUsers} pu 
left join {Tables.User} u on pu.PublicKey = u.PublicKey
where pu.PromotionId = {idVal}
")

                // Get a list of all CREDITS to USER - PAYABLE
                .Add($@"
select promoter.Id, promoter.PublicKey, promoter.Handle
from {Tables.GeneralLedger} gl
inner join {Tables.GeneralLedgerAccount} debit on debit.Id = gl.DebitAccountId
inner join {Tables.GeneralLedgerAccount} credit on credit.Id = gl.CreditAccountId
inner join {Tables.User} promoter on promoter.Id = credit.UserId
where debit.PromotionId = {idVal}
and credit.LedgerTypeId = {(int) GeneralLedgerType.Payable}");

            BuildProfile(builder, idVal);
        }

        public override Promotion Read(IDapperGridReader reader)
        {
            var promotion = reader
                .Read<PromotionHeaderRecord, BitCloutUser, Promotion>(Map.Promotion)
                .SingleOrDefault();

            if (promotion == null) return null;

            promotion.Events = reader
                .Map<EntityLogRecord, BitCloutUser>(Map.EntityLog)
                .Select(r => new EntityLog(r))
                .OrderBy(e => e.TimeStamp)
                .ToList();

            ReadAllowedUsers(reader, promotion);
            
            promotion.Promoters = reader.Read<BitCloutUser>().ToList();

            ReadProfile(reader, promotion.Client);
            return promotion;
        }

        protected void BuildProfile(IStatementBuilder builder, string idVal)
        {
            if (!_includeClientProfile) return;
            builder
                .Add($@"
select up.UserId, up.Role, up.Setting, up.Value
from {Tables.UserProfile} up
inner join {Tables.Promotion} p on p.UserId = up.UserId
where p.Id = {idVal}");
        }

        private void ReadAllowedUsers(IDapperGridReader reader, Promotion promotion)
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
                promotion.Criteria ??= new PromotionCriteriaModel();
                promotion.Criteria.AllowedUsers = g.Select(i => i.User.PublicKey).ToList();
                promotion.Inbox = g.ToList();
            }
        }

        protected void ReadProfile(IDapperGridReader reader, BitCloutUser client)
        {
            if (!_includeClientProfile) return;

            var settings = reader
                .Read<UserSettingRecord>()
                .Where(r => r.UserId == client.Id)
                .Select(r => r.ToSetting());
            client.Profile = new UserProfile(settings);
        }
    }
}