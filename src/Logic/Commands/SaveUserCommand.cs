using System;

namespace CloutCast.Commands
{
    public interface ISaveUserCommand : IDapperCommand
    {
        void With(string handle, string publicKey, long appId);
    }

    public class SaveUserCommand : DapperCommand, ISaveUserCommand
    {
        private string _handle;
        private string _publicKey;
        private long _appId;
        public void With(string handle, string publicKey, long appId)
        {
            _handle = handle;
            _publicKey = publicKey;
            _appId = appId;
        }
        
        public override void Build(IStatementBuilder builder)
        {
            builder
                .Param("TimeStamp", DateTimeOffset.UtcNow)
                .Param("PublicKey", _publicKey)
                .Param("Handle", _handle)
                .TableParam("UserIds", "Id bigint")

                .Add($@"
MERGE {Tables.User} as target
USING (SELECT @PublicKey, @Handle) AS source (PublicKey, Handle)  
ON (target.PublicKey = source.PublicKey)  
WHEN MATCHED THEN
    UPDATE SET Handle = source.Handle  
WHEN NOT MATCHED THEN  
    INSERT (PublicKey, Handle, BlackList) 
    VALUES (source.PublicKey, source.Handle, 0) 
OUTPUT inserted.Id INTO @UserIds;")

                .Add($@"
INSERT INTO {Tables.EntityLog} (TimeStamp, Action, AppId, UserId)
SELECT @TimeStamp, {(int) EntityAction.UserRegister}, {_appId}, u.Id
FROM @UserIds u
LEFT JOIN {Tables.EntityLog} el on el.UserId = u.Id and el.Action = {(int) EntityAction.UserRegister}
WHERE el.Id IS NULL")

                .Add($@"
INSERT INTO {Tables.GeneralLedgerAccount} (LedgerTypeId, UserId)
SELECT glt.Id, u.Id
FROM {Tables.GeneralLedgerType} glt
CROSS JOIN {Tables.User} u
LEFT OUTER JOIN {Tables.GeneralLedgerAccount} gla on gla.UserId = u.Id and gla.LedgerTypeId = glt.Id
WHERE u.PublicKey = @PublicKey 
AND gla.Id IS NULL");
        }
    }
}