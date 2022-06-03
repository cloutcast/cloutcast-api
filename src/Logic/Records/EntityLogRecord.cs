namespace CloutCast.Records
{
    using Contracts;
    using Entities;
    using Models;

    internal class EntityLogRecord : EntityLogModel<BitCloutUser>, IEntity
    {
        public long Id { get; set; }
        public long PromotionId { get; set; }
    }
}