using System.Collections.Generic;
using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    using Entities;
    using Models;

    public class GetGLByRequest : ValidatedRequest<GetGLByRequest, List<GeneralLedgerItem>>
    {
        private GLAccountOwnerModel _accountOwner;

        public void AccountOwner(IGeneralLedgerAccountOwner account) =>
            _accountOwner = account is GLAccountOwnerModel model 
                ? model 
                : new GLAccountOwnerModel(account);

        public void AccountOwner(GeneralLedgerAccountType accountType, long accountId)
            => _accountOwner = new GLAccountOwnerModel(accountId, accountType);

        public IGeneralLedgerAccountOwner AccountOwner() => _accountOwner;

        protected override void SetupValidation(RequestValidator validator)
        {
            validator.RuleFor(req => req._accountOwner).NotNull();
            validator.RuleFor(req => req._accountOwner).GLAccountOwner();
        }
    }
}