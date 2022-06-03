using System;
using System.Collections.Generic;

namespace CloutCast.Records
{
    using Contracts;
    using Models;

    internal class PromotionHeaderRecord : PromotionHeaderModel, IPromotionCriteria, IPromotionTarget
    {
        public long Id { get; set; }

        //Criteria
        public long MinCoinPrice { get; set; }
        public int MinFollowerCount { get; set; }
        public List<string> AllowedUsers { get; set; }
            
        //Target
        public string TargetHex { get; set; }
        public PromotionActivity TargetAction { get; set; }
        public DateTimeOffset? TargetCreationDate { get; set; }
        public string TargetPost { get; set; }
            
        #region IPromotionTarget
        PromotionActivity IPromotionTarget.Action => TargetAction;
        DateTimeOffset? IPromotionTarget.CreationDate => TargetCreationDate;
        string IPromotionTarget.Hex=> TargetHex;
        #endregion
    }
}