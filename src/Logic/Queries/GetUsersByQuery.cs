using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using JetBrains.Annotations;

namespace CloutCast.Queries
{
    using Entities;
    using Models;
    using Records;

    public interface IGetUsersByQuery : IDapperQuery<List<BitCloutUser>>
    {
        IGetUsersByQuery IncludeProfile(bool include);
        IGetUsersByQuery IncludeUnRegisteredUsers(bool include);
        IGetUsersByQuery PublicKeys(IEnumerable<string> publicKeys);
        IGetUsersByQuery SaveUnRegisteredUsers(bool save);
    }

    [UsedImplicitly]
    public class GetUsersByQuery : ValidatedDapperQuery<GetUsersByQuery, List<BitCloutUser>>, IGetUsersByQuery
    {
        private bool _includeProfile = false;
        private bool _includeUnRegisteredUsers = false;
        private IEnumerable<string> _publicKeys;
        private bool _saveUnRegisteredUsers = false;

        public override void Build(IStatementBuilder builder)
        {
            builder
                .Table("PaymentUsers", Tables.UserTableType, _publicKeys, m => m
                    .Map("Id", publicKey => 0)
                    .String("PublicKey", 58, publicKey => publicKey));
            
            BuildInsertUnRegisteredUsers(builder);
            BuildHeader(builder);
            BuildProfile(builder);
        }

        public override List<BitCloutUser> Read(IDapperGridReader reader)
        {
            var users = reader.Read<BitCloutUser>().ToList();
            if (!_includeProfile) return users;

            var groupedSettings = reader.Read<UserSettingRecord>().GroupBy(r => r.UserId).ToList();
            foreach (var user in users)
            {
                var settings = groupedSettings?
                    .Where(g => g.Key == user.Id)
                    .SelectMany(x => x)
                    .Select(x => x.ToSetting());
                user.Profile = new UserProfile(settings);
            }
            return users;
        }

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(req => req._publicKeys).NotEmpty();
            v.RuleForEach(req => req._publicKeys).MinimumLength(1).MaximumLength(58);
        }
        
        #region IGetUsersByQuery
        public IGetUsersByQuery IncludeProfile(bool include) => this.Fluent(x => _includeProfile = include);
        public IGetUsersByQuery IncludeUnRegisteredUsers(bool include) => this.Fluent(x => _includeUnRegisteredUsers = include);
        public IGetUsersByQuery PublicKeys(IEnumerable<string> publicKeys) => this.Fluent(x => _publicKeys = publicKeys);
        public IGetUsersByQuery SaveUnRegisteredUsers(bool save) => this.Fluent(x => _saveUnRegisteredUsers = save);
        #endregion

        #region QueryBuilding
        protected void BuildHeader(IStatementBuilder builder)
        {
            builder.Append($@"
select bu.Id, bu.PublicKey, bu.Handle, bu.BlackList
from {Tables.User} bu 
inner join @PaymentUsers pu on bu.PublicKey = pu.PublicKey");

            if (!_includeUnRegisteredUsers)
                builder.Append($@"
inner join {Tables.EntityLog} el on el.UserId = bu.Id
where el.Action = {(int) EntityAction.UserRegister}");
        }

        protected void BuildProfile(IStatementBuilder builder)
        {
            if (!_includeProfile) return;
            builder
                .Append($@"
select up.UserId, up.Role, up.Setting, up.Value
from {Tables.UserProfile} up
inner join {Tables.User} bu on bu.Id = up.UserId
inner join @PaymentUsers pu on bu.PublicKey = pu.PublicKey");

            if (!_includeUnRegisteredUsers)
                builder.Append($@"
inner join {Tables.EntityLog} el on el.UserId = bu.Id
where el.Action = {(int) EntityAction.UserRegister}");

        }

        protected void BuildInsertUnRegisteredUsers(IStatementBuilder builder)
        {
            if (!_saveUnRegisteredUsers) return;
            builder
                .TableParam("NewUsers", "Id bigint, PublicKey nvarchar(58)")
                .Add($@"
INSERT INTO {Tables.User} (PublicKey, Handle)
OUTPUT inserted.Id as Id, inserted.PublicKey into @NewUsers
SELECT pu.PublicKey, 'UnVerified - ' + pu.PublicKey
FROM @PaymentUsers pu
LEFT OUTER JOIN {Tables.User} u on u.PublicKey = pu.PublicKey
WHERE u.Id IS NULL

INSERT INTO {Tables.GeneralLedgerAccount} (LedgerTypeId, UserId)
SELECT glt.Id, nu.Id
FROM {Tables.GeneralLedgerType} glt
CROSS JOIN @NewUsers nu 
LEFT OUTER JOIN {Tables.GeneralLedgerAccount} gla on gla.UserId = nu.Id and gla.LedgerTypeId = glt.Id
WHERE gla.Id IS NULL");
        }
        #endregion
    }
}