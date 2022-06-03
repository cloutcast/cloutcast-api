namespace CloutCast.Entities
{
    using Contracts;
    using Models;

    public class EntityLog : EntityLogModel<BitCloutUser>, IEntity
    {
        public EntityLog() {}
        public EntityLog(IEntityLog source):base(source)
        {
            if (source is IEntity entity) Id = entity.Id;
        }

        public long Id { get; set; }
    }
}