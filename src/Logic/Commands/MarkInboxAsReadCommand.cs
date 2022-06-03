using System;
using FluentValidation;
using JetBrains.Annotations;
using PureLogicTek;

namespace SkunkWerx.CloutCast.Commands
{
    using Contracts;

    [UsedImplicitly]
    public class MarkInboxAsReadCommand : ValidatedDapperCommand<MarkInboxAsReadCommand>
    {
        public long PromotionId { get; set; }
        public IBitCloutUser User { get; set; }

        public override void Build(IStatementBuilder builder) => builder
            .Param("ReadOn", DateTimeOffset.UtcNow)
            .Param("UserKey", User.PublicKey)
            .Add($@"
UPDATE {Tables.PromotionUsers} 
set ReadOn = @ReadOn 
where PromotionId = {PromotionId}  
and PublicKey = @UserKey");

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(cmd => cmd.PromotionId).GreaterThan(0);
            v.RuleFor(cmd => cmd.User).NotNull().BitCloutUser();
        }
    }
}