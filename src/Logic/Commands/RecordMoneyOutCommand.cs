using FluentValidation;

namespace CloutCast.Commands
{
    public interface IRecordMoneyOutCommand : IDapperCommand, IValidated
    {
        void Receipt(long entityLogId, string postHex);
    }

    public class RecordMoneyOutCommand : ValidatedDapperCommand<RecordMoneyOutCommand>, IRecordMoneyOutCommand
    {
        private long _entityLogId;
        private string _postHex;

        public void Receipt(long entityLogId, string postHex)
        {
            _entityLogId = entityLogId;
            _postHex = postHex;
        }

        public override void Build(IStatementBuilder builder) => builder
            .Param("PostHex", _postHex)
            .Add($@"
insert into {Tables.Evidence} (EntityLogId, PostHex)
select {_entityLogId}, @PostHex
where not exists (select 1 from {Tables.Evidence} where EntityLogId = {_entityLogId})");

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(cmd => cmd._entityLogId).GreaterThan(0);
            v.RuleFor(cmd => cmd._postHex).NotEmpty().MaximumLength(64);
        }
    }
}