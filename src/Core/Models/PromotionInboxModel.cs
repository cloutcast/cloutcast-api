using System;
using SkunkWerx.CloutCast.Contracts;

namespace SkunkWerx.CloutCast.Models
{
    public class PromotionInboxModel
    {
        public IBitCloutUser User { get; set; }
        public DateTimeOffset? ReadOn { get; set; }
    }
}