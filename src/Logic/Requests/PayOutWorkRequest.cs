using System;
using System.Collections.Generic;
using FluentValidation;

namespace CloutCast.Requests
{
    using Entities;

    public class PayOutWorkRequest : ValidatedRequest<PayOutWorkRequest>
    {
        public DateTimeOffset AsOf { get; set; }
        public List<GeneralLedgerItem> LedgerItems { get; set; } 

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(req => req.AsOf).GreaterThan(DateTimeOffset.MinValue);
        }
    }
}