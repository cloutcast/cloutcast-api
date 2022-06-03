namespace CloutCast.Commands
{
    public interface IReverseGeneralLedgerCommand : IDapperCommand
    {
        IReverseGeneralLedgerCommand SourceId(long sourceId);
        IReverseGeneralLedgerCommand SourceParam(string sourceParam);
        IReverseGeneralLedgerCommand TargetId(long targetId);
        IReverseGeneralLedgerCommand TargetParam(string targetParam);
    }

    public class ReverseGeneralLedgerCommand : DapperCommand, IReverseGeneralLedgerCommand
    {
        private string _source;
        private string _target;

        public override void Build(IStatementBuilder builder) => builder.Add($@"
-- Reverse GeneralLedger Command
INSERT INTO {Tables.GeneralLedger} (EntityLogId, Amount, DebitAccountId, CreditAccountId, Memo)
SELECT {_target}, gl.Amount, gl.CreditAccountId, gl.DebitAccountId, 'Reversal ' + ISNULL(gl.Memo, '')
FROM  {Tables.GeneralLedger} gl
WHERE gl.EntityLogId = {_source}");

        #region IReverseGeneralLedgerCommand
        public IReverseGeneralLedgerCommand SourceId(long sourceId)
        {
            if (sourceId > 0) _source = $"{sourceId}";
            return this;
        }
        public IReverseGeneralLedgerCommand SourceParam(string sourceParam)
        {
            if (sourceParam.IsNotEmpty()) _source = $"@{sourceParam.Replace("@", "")}";
            return this;
        }
        public IReverseGeneralLedgerCommand TargetId(long targetId)
        {
            if (targetId > 0) _target = $"{targetId}";
            return this;
        }
        public IReverseGeneralLedgerCommand TargetParam(string targetParam)
        {
            if (targetParam.IsNotEmpty()) _target = $"@{targetParam.Replace("@", "")}";
            return this;
        }
        #endregion
    }
}