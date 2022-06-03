namespace CloutCast
{
    using Entities;
    using Models;
    using Records;

    internal static class Map
    {
        internal static void EntityLog(EntityLogRecord record, BitCloutUser user)
        {
            if (record != null) record.User = user;
        }

        internal static Promotion Promotion(PromotionHeaderRecord header, BitCloutUser client)
        {
            if (header == null) return null;
            return new Promotion
            {
                Id = header.Id,
                Header = header.Clone<PromotionHeaderModel>(),
                Client = client,
                Criteria = new PromotionCriteriaModel
                {
                    MinCoinPrice = header.MinCoinPrice,
                    MinFollowerCount = header.MinFollowerCount
                },
                Target = new PromotionTargetModel(header)
            };
        }
    }
}