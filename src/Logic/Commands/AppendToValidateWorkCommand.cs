using System;
using FluentValidation;

namespace CloutCast.Commands
{
    public interface IAppendToValidateWorkCommand : IDapperCommand
    {
        IAppendToValidateWorkCommand CheckOn(DateTimeOffset checkOn);
        void EntityLogParam(string entityLogIdParam);
    }
    
    public class AppendToValidateWorkCommand : ValidatedDapperCommand<AppendToValidateWorkCommand>, IAppendToValidateWorkCommand
    {
        private string _idParam = "";
        private DateTimeOffset? _checkOn;

        public override void Build(IStatementBuilder builder)
        {
            builder
                .Param("CheckOn", _checkOn ?? DateTimeOffset.UtcNow.AddDays(2))
                .Add($"insert into {Tables.ValidateWork} (EntityLogId, CheckOn) values ({_idParam}, @CheckOn)");
        }

        public IAppendToValidateWorkCommand CheckOn(DateTimeOffset checkOn) => this.Fluent(x => _checkOn = checkOn);

        public void EntityLogParam(string entityLogIdParam) => this.Fluent(x => _idParam = $"@{entityLogIdParam.Replace("@", "")}");
        
        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(cmd => cmd._idParam).NotEmpty().Must(e => e.StartsWith("@"));
            v.RuleFor(cmd => cmd._checkOn).NotNull();
        }
    }
}