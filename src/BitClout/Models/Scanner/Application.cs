using System.Collections.Generic;

namespace CloutCast.Models.Scanner
{
    public class Application
    {
        public string Error { get; set; }
        public List<Transactions> Transactions { get; set; }
        public ulong BalanceNanos { get; set; }
    }
}