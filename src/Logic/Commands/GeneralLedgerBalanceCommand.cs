using System;

namespace CloutCast.Commands
{
    public interface IGeneralLedgerBalanceCommand : IDapperCommand
    {
        IGeneralLedgerBalanceCommand Account(GeneralLedgerAccountType accountType, long id);
        IGeneralLedgerBalanceCommand Account(GeneralLedgerAccountType accountType, string paramName);
        IGeneralLedgerBalanceCommand Action(GeneralLedgerAction action);
        IGeneralLedgerBalanceCommand AsOf(DateTimeOffset asOf);
        IGeneralLedgerBalanceCommand BalanceParam(string paramName);
        IGeneralLedgerBalanceCommand Ledger(GeneralLedgerType ledger);
    }

    public class GeneralLedgerBalanceCommand : DapperCommand, IGeneralLedgerBalanceCommand
    {
        private GeneralLedgerAction _action = GeneralLedgerAction.Undefined;
        private DateTimeOffset? _asOf;
        private string _balanceParam;
        private readonly GLClauseBuilder _clause = new GLClauseBuilder();
        
        public override void Build(IStatementBuilder builder)
        {
            builder
                .Shared(_balanceParam, 0L)
                .Append($@"
SELECT @{_balanceParam} = ISNULL(SUM(gl.Amount), 0)
FROM {Tables.GeneralLedger} gl
INNER JOIN {Tables.GeneralLedgerAccount} {_action} on gl.{_action}AccountId = {_action}.Id");

            if (_asOf != null)
                builder
                    .Param("AsOf", _asOf.Value)
                    .Append($"INNER JOIN {Tables.EntityLog} el on gl.EntityLogId = el.Id AND el.TimeStamp <= @AsOf");
            
            builder.Add($"WHERE {_clause.ToClause(_action.ToDescription())}");
        }

        public IGeneralLedgerBalanceCommand Account(GeneralLedgerAccountType accountType, long id)
        {
            _clause.AccountType = accountType;
            _clause.Param = $"{id}";
            return this;
        }

        public IGeneralLedgerBalanceCommand Account(GeneralLedgerAccountType accountType, string paramName) 
        {
            _clause.AccountType = accountType;
            _clause.Param = $"@{paramName}";
            return this;
        }

        public IGeneralLedgerBalanceCommand Action(GeneralLedgerAction action) => this.Fluent(x => _action = action);
        public IGeneralLedgerBalanceCommand AsOf(DateTimeOffset asOf) => this.Fluent(x => _asOf = asOf);
        public IGeneralLedgerBalanceCommand BalanceParam(string paramName) => this.Fluent(x => _balanceParam = paramName);
        public IGeneralLedgerBalanceCommand Ledger(GeneralLedgerType ledger) => this.Fluent(x => _clause.Ledger = ledger);
    }
}