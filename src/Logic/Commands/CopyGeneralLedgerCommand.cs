namespace CloutCast.Commands
{
    public interface ICopyGeneralLedgerCommand : IDapperCommand
    {
        ICopyGeneralLedgerCommand SourceId(long sourceId);
        ICopyGeneralLedgerCommand TargetParam(string targetParam);
    }

    public class CopyGeneralLedgerCommand : DapperCommand,ICopyGeneralLedgerCommand 
    {
        private string _source;
        private string _target;

        public override void Build(IStatementBuilder builder) => builder.Add($@"
-- Copy GeneralLedger Command
INSERT INTO {Tables.GeneralLedger} (EntityLogId, Amount, DebitAccountId, CreditAccountId, Memo)
SELECT {_target}, gl.Amount, gl.DebitAccountId, gl.CreditAccountId, gl.Memo
FROM  {Tables.GeneralLedger} gl
WHERE gl.EntityLogId = {_source}");

        public ICopyGeneralLedgerCommand SourceId(long sourceId)
        {
            if (sourceId > 0) _source = $"{sourceId}";
            return this;
        }
        public ICopyGeneralLedgerCommand TargetParam(string targetParam)
        {
            if (targetParam.IsNotEmpty()) _target = $"@{targetParam.Replace("@", "")}";
            return this;
        }

    }
}