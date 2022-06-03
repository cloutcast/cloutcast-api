using System.Collections.Generic;
using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;

    public class BitCloutTransactionScanRequest : ValidatedRequest<BitCloutTransactionScanRequest, List<IBitCloutFundingTransaction>>
    {
        public string WalletPublicKey { get; set; }

        protected override void SetupValidation(RequestValidator validator) => validator
            .RuleFor(req => req.WalletPublicKey)
            .NotEmpty()
            .WithMessage("Missing Wallet PublicKey");
    }
}