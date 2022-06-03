using System;
using System.Collections.Generic;
using MediatR;

namespace CloutCast.Requests
{
    using Entities;

    public class GetPendingValidationsRequest : IRequest<List<GeneralLedgerItem>>
    {
        public DateTimeOffset AsOf { get; set; }
    }
}