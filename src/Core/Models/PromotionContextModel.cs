using System.Collections.Generic;

namespace CloutCast.Models
{
    using Contracts;

    public class PromotionContextModel
    {
        public IBitCloutUser ActiveUser { get; set; }
        
        public bool IsActive { get; set; }

        public bool? IsUserAllowed { get; set; }
        
        public List<string> RejectionReasons { get; set; }

        public bool? SpecifiedByName { get; set; }
    }
}