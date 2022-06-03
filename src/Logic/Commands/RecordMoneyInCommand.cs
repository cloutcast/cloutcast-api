using System;
using System.Collections.Generic;
using FluentValidation;

namespace CloutCast.Commands
{
    using Contracts;
    using Models;
    
    public interface IRecordMoneyInCommand : IDapperCommand, IValidated
    {
        void Fund(IBitCloutUser user, List<BitCloutIncomingFunds> funds);
    }

    [JetBrains.Annotations.UsedImplicitly]
    public class RecordMoneyInCommand : ValidatedDapperCommand<RecordMoneyInCommand>, IRecordMoneyInCommand
    {
        class IncomeRecord : BitCloutIncomingFunds
        {
            public IBitCloutUser User { get; set; }
        }

        private readonly List<IncomeRecord> _records = new List<IncomeRecord>();

        public void Fund(IBitCloutUser user, List<BitCloutIncomingFunds> funds)
        {
            foreach (var fund in funds)
            {
                _records.Add(new IncomeRecord
                {
                    Amount = fund.Amount,
                    EvidencePostHex = fund.EvidencePostHex,
                    User = user,
                    UserPublicKey = user.PublicKey
                });
            }
        }

        public override void Build(IStatementBuilder builder)
        {
            builder
                .Shared("TimeStamp", DateTimeOffset.UtcNow)
                .Shared("AppId", 0L).Add($"SELECT @AppId = Id FROM {Tables.App} WHERE Name = 'System'")
                .TableParam("NewFunds", "EntityLogId bigint, UserId bigint, Amount bigint, PostHex nchar(64)", true);

            for (var i = 0; i < _records.Count; i++)
            {
                var evidenceHexParam = $"EvidenceHex_{i + 1}";
                var rec = _records[i];
                builder
                    .Param(evidenceHexParam, rec.EvidencePostHex)
                    .If($"select * from {Tables.Evidence} where PostHex = @{evidenceHexParam}", "not exists")
                    .Add($@"
INSERT INTO {Tables.EntityLog} (TimeStamp, Action, AppId, UserId)
OUTPUT inserted.Id as EntityLogId, {rec.User.Id} as UserId, {rec.Amount} as Amount, @{evidenceHexParam} as PostHex into @NewFunds
VALUES (@TimeStamp, {(int) EntityAction.UserAddFunds}, @AppId, {rec.User.Id})")
                    .EndIf();

                if (i < _records.Count - 1) continue;

                builder
                    .TableParam("LedgerIds", "Id bigint")
                    .Add($@"
INSERT INTO {Tables.GeneralLedger} (EntityLogId, Amount, DebitAccountId, CreditAccountId)
OUTPUT inserted.Id as Id into @LedgerIds
SELECT nf.EntityLogId, nf.Amount, da.Id, ca.Id
FROM @NewFunds nf
INNER JOIN {Tables.GeneralLedgerAccount} da on da.UserId = nf.UserId and da.LedgerTypeId = {(int)GeneralLedgerType.Cash}
INNER JOIN {Tables.GeneralLedgerAccount} ca on ca.UserId = da.UserId and ca.LedgerTypeId = {(int)GeneralLedgerType.Deposit}

INSERT INTO Evidence (EntityLogId, PostHex) 
SELECT nf.EntityLogId, nf.PostHex 
FROM @NewFunds nf ");
            }
        }

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(req => req._records).NotEmpty();
            v.RuleForEach(req => req._records).ChildRules(r =>
            {
                r.RuleFor(x => x.User).NotNull();
                r.RuleFor(x => x.User).BitCloutUser();
                r.RuleFor(x => x).Must(x => x.IsInput()).WithMessage("Only income funding events supported");
                r.RuleFor(x => x.UserPublicKey)
                    .Equal(x => x.User.PublicKey)
                    .WithMessage("Funding user does not match the funding event");
            });
        }
    }
}