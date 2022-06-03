using System.Linq;
using JetBrains.Annotations;

namespace CloutCast.Queries
{
    using Entities;
    using Models;
    using Records;

    public interface IGetBitCloutUserQuery : IDapperQuery<BitCloutUser>
    {
        IGetBitCloutUserQuery IncludeProfile(bool include);
        IGetBitCloutUserQuery PublicKey(string publicKey);
        IGetBitCloutUserQuery UserId(long userId);
    }

    [UsedImplicitly]
    public class GetBitCloutUserQuery : DapperQuery<BitCloutUser>, IGetBitCloutUserQuery
    {
        #region IGetBitCloutUserQuery
        private bool _includeProfile = true;
        private string _publicKey;
        private long _userId;

        public IGetBitCloutUserQuery IncludeProfile(bool include) => this.Fluent(x => _includeProfile = include);
        public IGetBitCloutUserQuery PublicKey(string publicKey) => this.Fluent(x => _publicKey = publicKey);
        public IGetBitCloutUserQuery UserId(long userId) => this.Fluent(x => _userId = userId);
        #endregion

        public override void Build(IStatementBuilder builder)
        {
            BuildHeader(builder);
            if (_includeProfile) BuildProfile(builder);
        }

        private void BuildHeader(IStatementBuilder builder)
        {
            builder
                .Append($@"
select bu.Id, bu.PublicKey, bu.Handle, bu.BlackList
from {Tables.User} bu 
inner join {Tables.EntityLog} el on el.UserId = bu.Id
where el.Action = {(int) EntityAction.UserRegister}");

            if (_userId > 0)
                builder.Add($"and bu.Id = {_userId }");
            else
                builder
                    .Param("PublicKey", _publicKey)
                    .Add("and bu.PublicKey = @PublicKey");
        }

        private void BuildProfile(IStatementBuilder builder) => builder
            .Append($@"
select up.UserId, up.Role, up.Setting, up.Value
from {Tables.UserProfile} up")
            .Add(_userId > 0
                ? $"where up.UserId = {_userId}"
                : $@"
inner join {Tables.User} bu on bu.Id = up.UserId
where bu.PublicKey = @PublicKey");

        public override BitCloutUser Read(IDapperGridReader reader)
        {
            var user = reader.Read<BitCloutUser>().SingleOrDefault();
            if (user == null || !_includeProfile) return user;

            var settings = reader.Read<UserSettingRecord>().Select(s => s.ToSetting());
            user.Profile = new UserProfile(settings);
            return user;
        }
    }
}