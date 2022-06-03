using FluentValidation;

namespace CloutCast
{
    internal class GLClauseBuilder
    {
        public GLClauseBuilder() {}
        public GLClauseBuilder(GeneralLedgerAccountType accountType, long id,  GeneralLedgerType ledger)
        {
            AccountType = accountType;
            Ledger = ledger;
            Param = $"{id}";
        }
        public GLClauseBuilder(GeneralLedgerAccountType accountType, string paramName, GeneralLedgerType ledger)
        {
            AccountType = accountType;
            Ledger = ledger;
            Param = $"@{paramName}";
        }

        public GeneralLedgerAccountType AccountType { get; set; }
        public GeneralLedgerType Ledger { get; set; }
        public string Param { get; set; }

        public string ToClause(string alias)
        {
            var ledgerClause = $"{alias}.LedgerTypeId";
            ledgerClause += $" = {(int) Ledger}";
            return $"{alias}.{AccountType}Id = {Param} and {ledgerClause}";
        }

        public class Validator : AbstractValidator<GLClauseBuilder>
        {
            public Validator()
            {
                RuleFor(gl => gl.AccountType)
                    .Must(a => a != GeneralLedgerAccountType.Undefined)
                    .WithMessage("Must supply valid GeneralLedger Account Type");

                RuleFor(gl => gl.Ledger)
                    .Must(l => l != GeneralLedgerType.Undefined)
                    .WithMessage("Must supply valid GeneralLedger Type");

                RuleFor(gl => gl.Param).NotEmpty();
            }
        }

    }
}