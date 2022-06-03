namespace CloutCast.Entities
{
    using Contracts;

    public abstract class AbstractEntity<E> : IEntity where E: AbstractEntity<E>
    {
        public long Id { get; set; }
    }
}