using FluentValidation;
using JetBrains.Annotations;

namespace CloutCast.Commands
{
    [UsedImplicitly]
    public class SaveReceiptCommand : ValidatedDapperCommand<SaveReceiptCommand>
    {
        public long GeneralLedgerId { get; set; }
        public string EvidencePostHex { get; set; }

        public override void Build(IStatementBuilder builder) => builder
            .Param("PostHex", EvidencePostHex)
            .Add($@"
INSERT INTO {Tables.Evidence} (EntityLogId, PostHex) 
SELECT gl.EntityLogId, @PostHex 
FROM {Tables.GeneralLedger} gl
INNER JOIN {Tables.GeneralLedgerAccount} da on da.Id = gl.DebitAccountId and da.LedgerTypeId = {(int)GeneralLedgerType.Deposit}
INNER JOIN {Tables.GeneralLedgerAccount} ca on ca.Id = gl.CreditAccountId and ca.LedgerTypeId = {(int)GeneralLedgerType.Cash}
LEFT OUTER JOIN {Tables.Evidence} e on e.EntityLogId = gl.EntityLogId
WHERE gl.Id = {GeneralLedgerId}
AND e.EntityLogId IS NULL");

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(cmd => cmd.GeneralLedgerId).GreaterThan(0);
            v.RuleFor(cmd => cmd.EvidencePostHex).NotEmpty().MaximumLength(64);
        }
    }
}