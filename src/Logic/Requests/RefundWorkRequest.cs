using System;
using System.Collections.Generic;
using FluentValidation;

namespace CloutCast.Requests
{
    using Entities;
    public class RefundWorkRequest : ValidatedRequest<RefundWorkRequest>
    {
        public DateTimeOffset AsOf { get; set; }
        public List<GeneralLedgerItem> LedgerItems { get; set; }

        protected override void SetupValidation(RequestValidator v)
        {
            v.RuleFor(req => req.AsOf).GreaterThan(DateTimeOffset.MinValue);
        }
    }
}