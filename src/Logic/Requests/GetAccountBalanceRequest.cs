using System;
using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    using Models;

    public class GetAccountBalanceRequest : ValidatedRequest<GetAccountBalanceRequest, AccountBalanceModel>
    {
        private GLAccountOwnerModel _accountOwner;
        public IGeneralLedgerAccountOwner AccountOwner() => _accountOwner;
        public GetAccountBalanceRequest AccountOwner(GeneralLedgerAccountType type, long id) =>
            this.Fluent(x => _accountOwner = new GLAccountOwnerModel {Id = id, Type = type});

        private DateTimeOffset? _asOf;
        public DateTimeOffset? AsOf() => _asOf;
        public GetAccountBalanceRequest AsOf(DateTimeOffset asOf) => this.Fluent(x => _asOf = asOf);
        
        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(req => req._accountOwner).NotNull();
            v.RuleFor(req => req._accountOwner).GLAccountOwner();
        }
    }
}