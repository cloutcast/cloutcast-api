namespace CloutCast
{
    public interface IDapperStatement
    {
        void Build(IStatementBuilder builder);
    }

    public abstract class DapperStatement : IDapperStatement
    {
        public abstract void Build(IStatementBuilder builder);
    }
}