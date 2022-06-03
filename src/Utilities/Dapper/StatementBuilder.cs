using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Dapper;

namespace CloutCast
{
    public interface IStatementBuilderOutput
    {
        string Sql { get; }
        object Parameters { get; }

        bool HasStatement();
    }

    public interface IStatementBuilder : IDisposable
    {
        IStatementBuilder Add(string command);
        IStatementBuilder Append(string command);
        IStatementBuilder Param(string paramName, object paramValue);
        IStatementBuilder TableParam(string tableName, string columns, bool shared = false);

        IStatementBuilder Postfix(string postFix);
        IStatementBuilder Shared(string paramName, object paramValue);
        IStatementBuilder Table<T>(string paramName, string tableType, IEnumerable<T> source, Action<IDapperStatementToDataTableMap<T>> setupAction) where T : class;

        IStatementBuilder If(string clause, string exists = "");
        IStatementBuilder IfReturn(string clause, string exists = "");
        IStatementBuilder Else(string command);
        IStatementBuilder EndIf();

        IStatementBuilder Indent();
        IStatementBuilder UnIndent();

        IStatementBuilderOutput ToOutput();

        bool HasQuery { get; }
    }

    public class StatementBuilder : Disposable, IStatementBuilder
    {
        private readonly StringBuilder _fullSql = new StringBuilder();
        private readonly StringBuilder _indent = new StringBuilder();
        private readonly ExpandoObject _parameters = new ExpandoObject();
        private readonly List<string> _sharedTableParam = new List<string>();
        private readonly IDictionary<string, object> _currentParams = new Dictionary<string, object>();
        private string _postFix;

        public bool HasQuery => _fullSql.Length > 0;
        
        public IStatementBuilder Table<T>(string paramName, string tableType, IEnumerable<T> source, Action<IDapperStatementToDataTableMap<T>> setupAction) where T: class
        {
            var mapper = new DapperStatementToDataTableMap<T>();
            setupAction(mapper);
            var table = mapper.Fill(source);
            return Param(paramName, table.AsTableValuedParameter($"dbo.{tableType}"));
        }

        public IStatementBuilder Indent()
        {
            _indent.Append("\t");
            return this;
        }
        public IStatementBuilder UnIndent()
        {
            _indent.Remove(_indent.Length - 1, 1);
            return this;
        }

        public IStatementBuilder Add(string statement) => UpdateSql(statement?.Trim());
        public IStatementBuilder Append(string statement) => UpdateSql(statement?.Trim(), false);

        public IStatementBuilder Else(string command)
        {
            UnIndent();
            UpdateSql("END ELSE BEGIN", false).Indent();
            return UpdateSql(command, false);
        }
        public IStatementBuilder EndIf()
        {
            UnIndent();
            return UpdateSql("END");
        }
        public IStatementBuilder If(string clause, string exists="") => UpdateSql($"IF {exists}({clause}) BEGIN", false).Indent();
        public IStatementBuilder IfReturn(string clause, string exists = "") => UpdateSql($"IF {exists}({clause}) RETURN");

        public IStatementBuilder Postfix(string postFix) => this.Fluent(x => _postFix = postFix);
        public IStatementBuilder Param(string paramName, object paramValue) => this.Fluent(x => _currentParams[paramName.Trim()] = paramValue);
        public IStatementBuilder TableParam(string tableName, string columns, bool shared = false)
        {
            if (shared)
            {
                if (_sharedTableParam.Any(s => s.Equals(tableName, StringComparison.CurrentCultureIgnoreCase))) return this;
                _sharedTableParam.Add(tableName);
            }
            else
                _currentParams[tableName] = new SqlTableParamStub();
            return UpdateSql($"DECLARE @{tableName} table ({columns})"); //DECLARE @PromoIds table (Id bigint)
        }

        public IStatementBuilder Shared(string paramName, object paramValue) =>
            this.Fluent(x => ((IDictionary<string, object>) _parameters)[paramName.Trim()] = paramValue);

        public IStatementBuilderOutput ToOutput()
        {
            /*
             * Look into FormattableString for templates
             * https://dev.to/dealeron/advanced-string-templates-in-c-2eh2
             */
            _sharedTableParam.Clear();
            return new StatementBuilderOutput(ToString(), _parameters);
        }

        protected string UpdateParamsInStatement(string sql, string key, string newKey) => sql
            .Replace($"@{key} ", $"@{newKey} ")
            .Replace($"@{key},", $"@{newKey},")
            .Replace($"@{key}+", $"@{newKey}+")
            .Replace($"@{key}-", $"@{newKey}-")
            .Replace($"@{key})", $"@{newKey})")

            .Replace($"@{key}\r\n", $"@{newKey}\r\n")
            .Replace($"@{key}\n", $"@{newKey}\n")
            .Replace($"@{key};", $"@{newKey};");

        public void Parameterize()
        {
            var dict = (IDictionary<string, object>) _parameters;

            var sql = _fullSql.ToString();
            foreach (var key in _currentParams.Keys)
            {
                var newKey = $"{key}{_postFix}";
                var result = UpdateParamsInStatement(sql, key, newKey);
                if (result.Equals(sql)) continue;

                sql = result;
                //Do not store SqlTableParam Stubs
                if (_currentParams[key] is SqlTableParamStub) continue;
                dict[newKey] = _currentParams[key];
            }

            _fullSql.Clear();
            _fullSql.Append(sql);
            _currentParams.Clear();
        }
       
        public override string ToString() => _fullSql.ToString();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _indent.Clear();
            _fullSql.Clear();
        }

        private IStatementBuilder UpdateSql(string statement, bool newLine=true)
        {
            if (statement.IsEmpty()) return this;
            if (HasQuery) _fullSql.AppendLine(_indent.ToString());
            if (newLine)
                _fullSql.AppendLine($"{_indent}{statement.Replace("\n", $"\n{_indent}")}");
            else
                _fullSql.Append($"{_indent}{statement.Replace("\n", $"\n{_indent}")}");
            return this;
        }

        private class SqlTableParamStub { }
        private class StatementBuilderOutput: IStatementBuilderOutput
        {
            public StatementBuilderOutput(string sql, ExpandoObject parameters)
            {
                Sql = sql;
                Parameters = parameters;
            }
            public string Sql { get; }
            public dynamic Parameters { get; }

            public bool HasStatement() => Sql.IsNotEmpty();
        }
    }
}