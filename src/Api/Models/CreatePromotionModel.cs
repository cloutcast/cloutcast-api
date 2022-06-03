namespace CloutCast.Models
{        
    public class CreatePromotionModel 
    {
        public PromotionHeaderModel Header { get; set; }
        public PromotionCriteriaModel Criteria { get; set; }
        public PromotionTargetModel Target { get; set; }
    }
}