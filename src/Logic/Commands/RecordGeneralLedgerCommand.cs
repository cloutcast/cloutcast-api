using FluentValidation;

namespace CloutCast.Commands
{
    public interface IRecordGeneralLedgerCommand : IDapperCommand
    {
        IRecordGeneralLedgerCommand Amount(long amount);
        IRecordGeneralLedgerCommand Amount(string sql);

        IRecordGeneralLedgerCommand Credit(string paramName, GeneralLedgerAccountType account, GeneralLedgerType ledger);
        IRecordGeneralLedgerCommand Credit(long id, GeneralLedgerAccountType account, GeneralLedgerType ledger);

        IRecordGeneralLedgerCommand Debit(string paramName, GeneralLedgerAccountType account, GeneralLedgerType ledger);
        IRecordGeneralLedgerCommand Debit(long id, GeneralLedgerAccountType account, GeneralLedgerType ledger);

        IRecordGeneralLedgerCommand EntityLogParam(string entityLogIdParam);
        IRecordGeneralLedgerCommand EntityLogId(long entityLogId);

        IRecordGeneralLedgerCommand Memo(string memo);
        IRecordGeneralLedgerCommand ProofOfWork(string postHex);
    }

    [JetBrains.Annotations.UsedImplicitly]
    public class RecordGeneralLedgerCommand : ValidatedDapperCommand<RecordGeneralLedgerCommand>, IRecordGeneralLedgerCommand
    {
        internal GLClauseBuilder CreditClause;
        internal GLClauseBuilder DebitClause;
        private string _amountSql;
        private string _entityLogIdParam = "@NewEntityLogId";
        private string _memo;
        private string _postHex;

        public override void Build(IStatementBuilder builder)
        {
            InsertIntoGl(builder);
            InsertProofOfWork(builder);
        }

        #region Statements
        protected void InsertIntoGl(IStatementBuilder builder) => builder
            .Param("Memo", _memo)
            .Add($@"
INSERT INTO {Tables.GeneralLedger} (EntityLogId, Amount, DebitAccountId, CreditAccountId, Memo)
SELECT {_entityLogIdParam}, {_amountSql}, debit.Id, credit.Id, @Memo
FROM {Tables.GeneralLedgerAccount} debit
INNER JOIN {Tables.GeneralLedgerAccount} credit on {CreditClause.ToClause("credit")}
WHERE {DebitClause.ToClause("debit")}");

        protected void InsertProofOfWork(IStatementBuilder builder)
        {
            var paramName = "PostHex";
            if (_postHex.IsEmpty()) return;

            builder
                .Param(paramName, _postHex)
                .Add($@"
INSERT INTO {Tables.Evidence} (EntityLogId, PostHex) 
VALUES ({_entityLogIdParam}, @{paramName})");
        }
        #endregion

        #region IWriteToGeneralLedgerCommand
        public IRecordGeneralLedgerCommand Amount(long amount) => this.Fluent(x => _amountSql = $"{amount}");
        public IRecordGeneralLedgerCommand Amount(string sql) => this.Fluent(x => _amountSql = sql);

        public IRecordGeneralLedgerCommand Credit(string paramName, GeneralLedgerAccountType account, GeneralLedgerType ledger)
            => this.Fluent(x => CreditClause = new GLClauseBuilder(account, paramName, ledger));
        public IRecordGeneralLedgerCommand Credit(long id, GeneralLedgerAccountType account, GeneralLedgerType ledger)
            => this.Fluent(x => CreditClause = new GLClauseBuilder(account, id, ledger));

        public IRecordGeneralLedgerCommand Debit(string paramName, GeneralLedgerAccountType account, GeneralLedgerType ledger)
            => this.Fluent(x => DebitClause = new GLClauseBuilder(account, paramName, ledger));
        public IRecordGeneralLedgerCommand Debit(long id, GeneralLedgerAccountType account, GeneralLedgerType ledger)
            => this.Fluent(x => DebitClause = new GLClauseBuilder(account, id, ledger));

        public IRecordGeneralLedgerCommand EntityLogParam(string entityLogIdParam) => this.Fluent(x => _entityLogIdParam = $"@{entityLogIdParam.Replace("@", "")}");
        public IRecordGeneralLedgerCommand EntityLogId(long entityLogId) => this.Fluent(x => _entityLogIdParam = $"{entityLogId}");

        public IRecordGeneralLedgerCommand Memo(string memo) => this.Fluent(x => _memo = memo);

        public IRecordGeneralLedgerCommand ProofOfWork(string postHex) => this.Fluent(x => _postHex = postHex);
        #endregion

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(cmd => cmd._amountSql)
                .NotEmpty()
                .WithMessage("Must provide the amount to write to the General Ledger");

            v.RuleFor(cmd => cmd.CreditClause)
                .NotEmpty().WithMessage("Must provide Creditor")
                .SetValidator(new GLClauseBuilder.Validator());

            v.RuleFor(cmd => cmd.DebitClause)
                .NotEmpty().WithMessage("Must provide Debitor")
                .SetValidator(new GLClauseBuilder.Validator());
        }
    }
}