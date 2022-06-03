using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;

namespace CloutCast.Queries
{
    using Entities;
    using Models;

    public interface IGetGLQuery : IDapperQuery<List<GeneralLedgerItem>>, IValidated
    {
        void ForLedger(GeneralLedgerAction action, GeneralLedgerType ledger);
        void ForGeneralLedgerId(long glId);
        void ForPendingPayouts(DateTimeOffset asOf);
        void ForUserId(long userId, DateTimeOffset? asOf = null);
    }
    public class GetGLQuery : ValidatedDapperQuery<GetGLQuery, List<GeneralLedgerItem>>, IGetGLQuery
    {
        #region IGetGLQuery
        private bool _pendingPayouts;
        private DateTimeOffset? _asOf;
        private GeneralLedgerAction _action = GeneralLedgerAction.Undefined;
        private GeneralLedgerType _ledger = GeneralLedgerType.Undefined;
        private long _generalLedgerId;
        private long _userId;
        
        public void ForLedger(GeneralLedgerAction action, GeneralLedgerType ledger)
        {
            _action = action;
            _ledger = ledger;
        }
        public void ForGeneralLedgerId(long glId) => _generalLedgerId = glId;
        public void ForPendingPayouts(DateTimeOffset asOf)
        {
            _pendingPayouts = true;
            _asOf = asOf;
        }
        public void ForUserId(long userId, DateTimeOffset? asOf = null)
        {
            _userId = userId;
            _asOf = asOf;
        }
        #endregion

        #region ValidatedDapperQuery

        public override void Build(IStatementBuilder builder) => builder
            .Add($@"
SELECT gl.Id, gl.Amount, TRIM(gl.Memo) Memo, TRIM(e.PostHex) as EvidencePostHex,
       el.Id, el.Action, el.TimeStamp,
	   bu.Id, bu.PublicKey, bu.Handle,
{LedgerSelect(GeneralLedgerAction.Debit)},
{LedgerSelect(GeneralLedgerAction.Credit)}
FROM {Tables.GeneralLedger} gl
INNER JOIN {Tables.EntityLog} el on el.Id = gl.EntityLogId
INNER JOIN {Tables.User} bu on bu.Id = el.UserId
LEFT OUTER JOIN {Tables.Evidence} e on e.EntityLogId = el.Id
{LedgerJoins(GeneralLedgerAction.Debit)}
{LedgerJoins(GeneralLedgerAction.Credit)}
{WhereClause(builder)}");

        public override List<GeneralLedgerItem> Read(IDapperGridReader reader)
        {
            var ledgers = reader
                .Map<GeneralLedgerItem, EntityLog, BitCloutUser,
                    GLAccountLedgerModel, GLAccountLedgerModel>(
                    (gl, el, bu, da, ca) =>
                    {
                        if (gl == null) return;
                        if (el != null) el.User = bu;

                        gl.EntityLog = el;
                        gl.Debit = da;
                        gl.Credit = ca;
                    })
                .ToList();
            return ledgers;
        }

        protected override void SetupValidation(RequestValidator v) => v.RuleFor(qry => qry)
            .Must(q =>
            {
                var cnt = 0;
                if (q._pendingPayouts) cnt += 1;
                if (q.IsForLedger()) cnt += 1;
                if (q._generalLedgerId > 0) cnt += 1;
                if (q._userId > 0) cnt += 1;

                return cnt == 1;
            })
            .WithMessage("Must set only one query parameter");
        #endregion

        private bool IsForLedger() => _ledger != GeneralLedgerType.Undefined && _action != GeneralLedgerAction.Undefined;

        private static string LedgerSelect(GeneralLedgerAction action, string spacer = "\t") => $@"
{spacer}ISNULL({action}User.Id, {action}Promo.Id) AS Id,
{spacer}CASE 
{spacer}{spacer}WHEN {action}User.Id IS NULL THEN 'Promotion' 
{spacer}{spacer}WHEN {action}Promo.Id IS NULL THEN 'User' 
{spacer}END AS Type,
{spacer}ISNULL({action}User.PublicKey, {action}Promo.TargetHex) BitCloutIdentifier,
{spacer}{action}.LedgerTypeId as Ledger";

        private static string LedgerJoins(GeneralLedgerAction action) => $@"
INNER JOIN {Tables.GeneralLedgerAccount} {action} on {action}.Id = gl.{action}AccountId
LEFT JOIN {Tables.User} {action}User on {action}User.Id = {action}.UserId
LEFT JOIN {Tables.Promotion} {action}Promo on {action}Promo.Id = {action}.PromotionId";

        private string WhereClause(IStatementBuilder builder)
        {
            if (_pendingPayouts)
            {
                builder.Param("AsOf", _asOf?? DateTimeOffset.UtcNow);
                return $@"
INNER JOIN {Tables.ValidateWork} vw on vw.EntityLogId = gl.EntityLogId
WHERE vw.CheckOn <= @AsOf AND vw.Result IS NULL".Trim();
            }

            if (IsForLedger())
                return $"WHERE {_action}.LedgerTypeId = {(int) _ledger}";
            
            if (_generalLedgerId > 0) 
                return $"WHERE gl.Id = {_generalLedgerId}";

            if (_asOf == null) 
                return $"WHERE bu.Id = {_userId}";

            builder.Param("AsOf", _asOf.Value);
            return $"WHERE bu.Id = {_userId} AND el.TimeStamp <= @AsOf";
        }
    }
}