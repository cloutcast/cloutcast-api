using System;
using FluentValidation;
using JetBrains.Annotations;

namespace CloutCast.Commands
{
    using Contracts;

    public interface IAppendToEntityLogCommand : IDapperCommand
    {
        IAppendToEntityLogCommand AsOf(DateTimeOffset asOf, IAppSource app);
        IAppendToEntityLogCommand Log(EntityAction action, long userId);
        IAppendToEntityLogCommand Log(EntityAction action, long userId, long promotionId);
        IAppendToEntityLogCommand Log(EntityAction action, long userId, string promotionParam);
        IAppendToEntityLogCommand OutputParam(string paramName);
    }

    [UsedImplicitly]
    public class AppendToEntityLogCommand : ValidatedDapperCommand<AppendToEntityLogCommand>, IAppendToEntityLogCommand
    {
        public override void Build(IStatementBuilder builder) => builder
            .Param("AsOf", _asOf)
            .TableParam("NewLogIds", "Id bigint, Action smallint", true)
            .Shared(_outputParam, 0L)

            .Add(_promotionParam.IsEmpty()
                ? $@"
INSERT INTO {Tables.EntityLog} (TimeStamp, Action, Active, AppId, UserId)
OUTPUT inserted.Id, inserted.Action into @NewLogIds
VALUES (@AsOf, {(int) _action}, {GetActiveState()}, {_app.Id}, {_userId})"
                : $@"
INSERT INTO {Tables.EntityLog} (TimeStamp, Action, Active, AppId, UserId, PromotionId)
OUTPUT inserted.Id, inserted.Action into @NewLogIds
VALUES (@AsOf, {(int) _action}, {GetActiveState()}, {_app.Id}, {_userId}, {_promotionParam})")

            .Add($"SELECT @{_outputParam} = Max(Id) from @NewLogIds");

        private int GetActiveState()
        {
            switch (_action)
            {
                case EntityAction.PromotionStart:
                case EntityAction.PromotionExtend:
                    return 1;

                case EntityAction.PromotionExpire:
                case EntityAction.PromotionStop:
                    return -1;

                default: return 0;
            }
        }

        #region IAppendToEntityLogCommand
        private IAppSource _app;
        private EntityAction _action = EntityAction.UnDefined;
        private DateTimeOffset _asOf = DateTimeOffset.UtcNow;
        private string _promotionParam = null;
        private long _userId;
        private string _outputParam = "NewEntityLogId";

        public IAppendToEntityLogCommand AsOf(DateTimeOffset asOf, IAppSource app)
        {
            _asOf = asOf;
            _app = app;
            return this;
        }
        public IAppendToEntityLogCommand Log(EntityAction action, long userId) 
        {
            _action = action;
            _userId = userId;
            return this;
        }
        public IAppendToEntityLogCommand Log(EntityAction action, long userId, long promotionId)
        {
            _action = action;
            _userId = userId;
            _promotionParam = $"{promotionId}";
            return this;
        }
        public IAppendToEntityLogCommand Log(EntityAction action, long userId, string promotionParam)
        {
            _action = action;
            _userId = userId;
            _promotionParam = $"@{promotionParam.Replace("@", "")}";
            return this;
        }
        public IAppendToEntityLogCommand OutputParam(string paramName) => this.Fluent(x => _outputParam = paramName);
        #endregion

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(c => _app).NotNull().Validate();
            v.RuleFor(x => _action).NotEqual(EntityAction.UnDefined).WithMessage("Must define entity action");
            v.RuleFor(c => _asOf).GreaterThan(DateTimeOffset.MinValue);
            v.RuleFor(c => _userId).GreaterThan(0).WithMessage("Must provide User Id");
        }
    }
}