namespace CloutCast.Records
{
    public class ClientTotalPromotionsRecord
    {
        public long ClientId { get; set; }
        public long TotalPromotions { get; set; }
    }
    public class ClientPromoterPercentageRecord: ClientTotalPromotionsRecord
    {
        public long TotalPromotionsDone { get; set; }
        public decimal Percentage
        {
            get
            {
                if (TotalPromotionsDone <= 0 || TotalPromotions <= 0) return 0.0m;
                return (TotalPromotionsDone * 1.0m) / (TotalPromotions * 1.0m);
            }
        }
    }
}