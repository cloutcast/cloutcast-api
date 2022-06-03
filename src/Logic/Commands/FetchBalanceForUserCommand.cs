using System;
using System.Collections.Generic;

namespace CloutCast.Commands
{
    using Contracts;
    using Models;

    public interface IFetchBalanceForUserCommand : IDapperCommand
    {
        IFetchBalanceForUserCommand AccountOwner(IGeneralLedgerAccountOwner accountOwner);
        IFetchBalanceForUserCommand AccountOwner(GeneralLedgerAccountType type, long accountId);
        IFetchBalanceForUserCommand AsOf(DateTimeOffset asOf);
        IFetchBalanceForUserCommand TotalFor(GeneralLedgerType type, string sqlParam);
    }

    public class FetchBalanceForUserCommand : DapperCommand, IFetchBalanceForUserCommand
    {
        private DateTimeOffset? _asOf;
        private readonly Dictionary<GeneralLedgerType, string> _outputParams = new Dictionary<GeneralLedgerType, string>();
        private GLAccountOwnerModel _owner;

        public IFetchBalanceForUserCommand AccountOwner(IGeneralLedgerAccountOwner accountOwner)
        {
            _owner = accountOwner is GLAccountOwnerModel model
                ? model
                : new GLAccountOwnerModel(accountOwner);
            return this;
        }
        public IFetchBalanceForUserCommand AccountOwner(GeneralLedgerAccountType type, long accountId)
        {
            _owner = new GLAccountOwnerModel {Id = accountId, Type = type};
            return this;
        }
        public IFetchBalanceForUserCommand AsOf(DateTimeOffset asOf) => this.Fluent(x => _asOf = asOf);
        public IFetchBalanceForUserCommand TotalFor(GeneralLedgerType type, string sqlParam) => this.Fluent(x => _outputParams[type] = sqlParam);
        
        public override void Build(IStatementBuilder builder)
        {
            builder.Param("AsOf", _asOf ?? DateTimeOffset.UtcNow);

            foreach (var kvp in _outputParams)
                GetAllByLedger(builder, kvp.Key, kvp.Value);
        }

        private void GetAllByLedger(IStatementBuilder builder, GeneralLedgerType ledger, string outputParam) => builder
            .Shared(outputParam, 0L)
            .Append($@"
SELECT @{outputParam} = ISNULL(SUM(Amount), 0)
FROM (
	select sum(gl.Amount) as Amount
	from {Tables.GeneralLedger} gl
	inner join {Tables.GeneralLedgerAccount} gla on gla.Id = gl.CreditAccountId
	inner join EntityLog el ON el.Id = gl.EntityLogId and el.TimeStamp <= @AsOf
	where gla.{_owner.Type}Id = {_owner.Id} and gla.LedgerTypeId = {(int) ledger}
	union
	select sum(gl.Amount) * -1 as Amount
	from {Tables.GeneralLedger} gl
	inner join {Tables.GeneralLedgerAccount} gla on gla.Id = gl.DebitAccountId
	inner join EntityLog el ON el.Id = gl.EntityLogId and el.TimeStamp <= @AsOf
	where gla.{_owner.Type}Id = {_owner.Id} and  gla.LedgerTypeId = {(int) ledger}
) total");
    }
}