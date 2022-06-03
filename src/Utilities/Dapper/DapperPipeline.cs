using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Autofac;
using Dapper;
using log4net;

namespace CloutCast
{
    public interface IDapperPipeline
    {
        IDapperPipeline Command<C>(Action<C> setup = null) where C : IDapperCommand;
        IDapperPipeline Query<Q, R>(Action<R> publish, Action noResult = null) where Q : IDapperQuery<R>;
        IDapperPipeline Query<Q, R>(Action<Q> setup, Action<R> publish, Action noResult = null) where Q : IDapperQuery<R>;

        IDapperPipeline Run();

        IDapperPipeline UseIsolationLevel(IsolationLevel level);
        IDapperPipeline UseTimeout(int commandTimeout);
    }

    public interface IDapperGridReader
    {
        IEnumerable<T0> Map<T0, T1>(Action<T0, T1> map);
        IEnumerable<T0> Map<T0, T1, T2>(Action<T0, T1, T2> map);
        IEnumerable<T0> Map<T0, T1, T2, T3>(Action<T0, T1, T2, T3> map);
        IEnumerable<T0> Map<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> map);
        IEnumerable<T0> Map<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> map);
        IEnumerable<T0> Map<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> map);

        IEnumerable<R> Read<R>();
        IEnumerable<R> Read<T0, T1, R>(Func<T0, T1, R> map);
        IEnumerable<R> Read<T0, T1, T2, R>(Func<T0, T1, T2, R> map);
        IEnumerable<R> Read<T0, T1, T2, T3, R>(Func<T0, T1, T2, T3, R> map);
        IEnumerable<R> Read<T0, T1, T2, T3, T4, R>(Func<T0, T1, T2, T3, T4, R> map);
        IEnumerable<R> Read<T0, T1, T2, T3, T4, T5, R>(Func<T0, T1, T2, T3, T4, T5, R> map);
        IEnumerable<R> Read<T0, T1, T2, T3, T4, T5, T6,R>(Func<T0, T1, T2, T3, T4, T5, T6, R> map);
    }
    
    public partial class DapperPipeline : IDapperPipeline
    {
        protected internal interface IStatementContext
        {
            void Build(StatementBuilder builder);

            void Read(IDapperGridReader reader);
        }

        private readonly IComponentContext _container;
        private readonly Func<IDbConnection> _dbConnectionBuilder;
        private readonly ILog _log;
        private readonly List<IStatementContext> _contexts;

        private bool _commandOnly;
        private int _timeout;
        private IsolationLevel _level;
        
        public DapperPipeline(IComponentContext container, ILog log)
        {
            _container = container;
            _dbConnectionBuilder = _container.Resolve<Func<IDbConnection>>();
            _contexts = new List<IStatementContext>();
            _log = log;
            Reset();
        }

        public IDapperPipeline Command<C>(Action<C> setup = null) where C : IDapperCommand
        {
            var command = _container.Resolve<C>();
            setup?.Invoke(command);
            _contexts.Add(new CommandContext<C>(command, _contexts.Count));
            return this;
        }

        public IDapperPipeline Query<Q, R>(Action<R> publish, Action noResult = null)
            where Q : IDapperQuery<R> => Query<Q, R>(q => { }, publish, noResult);

        public IDapperPipeline Query<Q, R>(Action<Q> setup, Action<R> publish, Action noResult = null) 
            where Q : IDapperQuery<R> 
        {
            _commandOnly = false;
            var query = _container.Resolve<Q>();
            setup?.Invoke(query);
            _contexts.Add(new QueryContext<R>(query, _contexts.Count, publish, noResult));
            return this;
        }

        public IDapperPipeline Run()
        {
            var output = Generate();
            if (output== null || !output.HasStatement())
            {
                Reset();
                return this;
            }

            using (var conn = OpenConnection())
            using (var tx = StartTransaction(conn, _level))
                try
                {
                    Execute(output, conn, tx);

                    if (_log.IsDebugEnabled) _log.Debug("Will Commit Transaction");
                    tx.Commit();
                    if (_log.IsDebugEnabled) _log.Debug("Did Commit Transaction");
                }
                catch (Exception ex)
                {
                    _log.Error($"Sql Processing; Caught Exception={ex.Message}");

                    try
                    {
                        if (_log.IsDebugEnabled) _log.Info("Will Roll Back Transaction");
                        tx.Rollback();
                        if (_log.IsDebugEnabled) _log.Info("Did Roll Back Transaction");
                    }
                    catch (Exception rollbackException)
                    {
                        _log.Error($"Rollback; Caught Exception={rollbackException.Message}");
                    }

                    throw;
                }
                finally
                {
                    Reset();
                }

            return this;
        }

        public IDapperPipeline UseTimeout(int timeout) => this.Fluent(x => _timeout = timeout);
        public IDapperPipeline UseIsolationLevel(IsolationLevel level) => this.Fluent(x =>
        {
            if (_level < level) _level = level;
        });

        protected internal IDbConnection OpenConnection()
        {
            if (_dbConnectionBuilder == null)
                throw new Exception("Missing Database Connection Builder");

            IDbConnection conn = null;
            try
            {
                conn = _dbConnectionBuilder();
                if (conn.State != ConnectionState.Open) conn.Open();
                return conn;
            }
            catch (Exception ex)
            {
                _log.Error($"Error Opening DB Connection; Ex={ex.Message}");
                conn?.Dispose();
                throw;
            }
        }
        protected internal IDbTransaction StartTransaction(IDbConnection conn, IsolationLevel level)
        {
            IDbTransaction tx = null;
            try
            {
                tx = conn.BeginTransaction(level);
                return tx;
            }
            catch (Exception ex)
            {
                _log.Error($"Error Opening DB Connection; Ex={ex.Message}");
                if (tx == null) throw;
                tx.Rollback();
                tx.Dispose();
                throw;
            }
        }

        protected internal void Execute(IStatementBuilderOutput output, IDbConnection conn, IDbTransaction tx)
        {
            if (_commandOnly)
                conn.Execute(output.Sql, output.Parameters, tx, _timeout);
            else
            {
                using (var gr = conn.QueryMultiple(output.Sql, output.Parameters, tx, _timeout, CommandType.Text))
                {
                    foreach (var context in _contexts.TakeWhile(context => !gr.IsConsumed))
                        context.Read(new DapperGridReader(gr));
                }
            }
        }

        protected internal IStatementBuilderOutput Generate()
        {
            using (var builder = new StatementBuilder())
            {
                foreach (var context in _contexts)
                    context.Build(builder);

                var output = builder.ToOutput();
                if (output.HasStatement())
                {
                    _log.Debug($@"sql-> {output.ToDebug()}");
                    return output;
                }

                _log.Info("No statement found to Process");
                return null;
            }
        }

        private void Reset()
        {
            _contexts.Clear();
            _commandOnly = true;
            _timeout = 120;
            _level = IsolationLevel.ReadUncommitted;
        }
    }
}