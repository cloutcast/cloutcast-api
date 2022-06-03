using System.Collections.Generic;

namespace CloutCast.Models
{
    [JetBrains.Annotations.UsedImplicitly]
    public class StakeEntry
    {
        public ulong TotalPostStake { get; set; }
        public List<object> StakeList { get; set; }
    }
}