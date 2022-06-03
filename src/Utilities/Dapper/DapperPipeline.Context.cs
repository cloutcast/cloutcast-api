using System;
using System.Collections.Generic;
using Dapper;

namespace CloutCast
{
    public partial class DapperPipeline
    {
        protected abstract class BaseContext<S> : IStatementContext where S : IDapperStatement
        {
            private readonly int _counter;
            protected readonly S Statement;

            protected BaseContext(S statement, int counter)
            {
                _counter = counter + 1;
                Statement = statement;
            }

            public void Build(StatementBuilder builder)
            {
                builder.Postfix($"{_counter:000}");

                if (Statement is IValidated validated)
                    validated.ValidateAndThrow();

                builder.Add($"-- {Statement.GetType().Name} --");
                Statement.Build(builder);
                builder.Parameterize();
            }

            public abstract void Read(IDapperGridReader reader);
        }

        protected class CommandContext<S> : BaseContext<S> where S : IDapperStatement
        {
            public CommandContext(S statement, int counter) : base(statement, counter) { }
            public override void Read(IDapperGridReader reader) { }
        }

        protected class QueryContext<R> : BaseContext<IDapperQuery<R>>
        {
            public QueryContext(IDapperQuery<R> query, int counter, Action<R> publishResult, Action noResult)
                : base(query, counter)
            {
                PublishResult = publishResult;
                NoResult = noResult;
            }

            public Action<R> PublishResult { get; }
            public Action NoResult { get; }

            public override void Read(IDapperGridReader reader)
            {
                var result = Statement.Read(reader);
                if (result != null)
                    PublishResult?.Invoke(result);
                else
                    NoResult?.Invoke();
            }
        }

        private class DapperGridReader : IDapperGridReader
        {
            private readonly SqlMapper.GridReader _gridReader;
            public DapperGridReader(SqlMapper.GridReader gr) => _gridReader = gr;
            
            #region Map
            public IEnumerable<T0> Map<T0, T1>(Action<T0, T1> map) => _gridReader.Read(
                new[] {typeof(T0), typeof(T1)},
                args =>
                {
                    map((T0) args[0], (T1) args[1]);
                    return (T0) args[0];
                });

            public IEnumerable<T0> Map<T0, T1, T2>(Action<T0, T1, T2> map) => _gridReader.Read(
                new[] {typeof(T0), typeof(T1), typeof(T2)},
                args =>
                {
                    map((T0) args[0], (T1) args[1], (T2) args[2]);
                    return (T0) args[0];
                });

            public IEnumerable<T0> Map<T0, T1, T2, T3>(Action<T0, T1, T2, T3> map) => _gridReader.Read(
                new[] {typeof(T0), typeof(T1), typeof(T2), typeof(T3)},
                args =>
                {
                    map((T0) args[0], (T1) args[1], (T2) args[2], (T3) args[3]);
                    return (T0) args[0];
                });

            public IEnumerable<T0> Map<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> map) => _gridReader.Read(
                new[] {typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
                args =>
                {
                    map((T0) args[0], (T1) args[1], (T2) args[2], (T3) args[3], (T4) args[4]);
                    return (T0) args[0];
                });

            public IEnumerable<T0> Map<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> map) => _gridReader.Read(
                new[] {typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) },
                args =>
                {
                    map((T0) args[0], (T1) args[1], (T2) args[2], (T3) args[3], (T4) args[4], (T5) args[5]);
                    return (T0) args[0];
                });

            public IEnumerable<T0> Map<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> map) =>
                _gridReader.Read(
                    new[]
                    {
                        typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4),
                        typeof(T5), typeof(T6)
                    },
                    args =>
                    {
                        map((T0) args[0], (T1) args[1], (T2) args[2], (T3) args[3], (T4) args[4],
                            (T5) args[5], (T6) args[6]);
                        return (T0) args[0];
                    });
            #endregion

            #region Read
            public IEnumerable<T> Read<T>() => _gridReader.Read<T>();

            public IEnumerable<R> Read<T0, T1, R>(Func<T0, T1, R> map) => _gridReader.Read(
                new[] {typeof(T0), typeof(T1)},
                args => map((T0) args[0], (T1) args[1]));

            public IEnumerable<R> Read<T0, T1, T2, R>(Func<T0, T1, T2, R> map) => _gridReader.Read(
                new[] {typeof(T0), typeof(T1), typeof(T2)},
                args => map((T0) args[0], (T1) args[1], (T2) args[2]));

            public IEnumerable<R> Read<T0, T1, T2, T3, R>(Func<T0, T1, T2, T3, R> map) => _gridReader.Read(
                new[] {typeof(T0), typeof(T1), typeof(T2), typeof(T3)},
                args => map((T0) args[0], (T1) args[1], (T2) args[2], (T3) args[3]));

            public IEnumerable<R> Read<T0, T1, T2, T3, T4, R>(Func<T0, T1, T2, T3, T4, R> map) => _gridReader.Read(
                new[] {typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4)},
                args => map((T0) args[0], (T1) args[1], (T2) args[2], (T3) args[3], (T4) args[4]));

            public IEnumerable<R> Read<T0, T1, T2, T3, T4, T5, R>(Func<T0, T1, T2, T3, T4, T5, R> map) =>
                _gridReader.Read(
                    new[] {typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)},
                    args => map((T0) args[0], (T1) args[1], (T2) args[2], (T3) args[3], (T4) args[4], (T5) args[5]));

            public IEnumerable<R> Read<T0, T1, T2, T3, T4, T5, T6, R>(Func<T0, T1, T2, T3, T4, T5, T6, R> map) =>
                _gridReader.Read(
                    new[]
                    {
                        typeof(T0), typeof(T1), typeof(T2), typeof(T3),
                        typeof(T4), typeof(T5), typeof(T6)
                    },
                    args => map(
                        (T0) args[0], (T1) args[1], (T2) args[2], (T3) args[3],
                        (T4) args[4], (T5) args[5], (T6) args[6]));

            #endregion
        }
    }
}