using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CloutCast
{
    public interface IDapperStatementToDataTableMap<out T> where T: class
    {
        IDapperStatementToDataTableMap<T> Map<V>(string colName, Func<T, V> getVal);
        IDapperStatementToDataTableMap<T> String(string colName, int maxLength, Func<T, string> getVal);
        IDapperStatementToDataTableMap<T> Primary<V>(string colName, Func<T, V> getVal);
        IDapperStatementToDataTableMap<T> Primary(string colName);
        IDapperStatementToDataTableMap<T> Primary(string colName, int maxLength, Func<T, string> getVal);
    }

    internal class DapperStatementToDataTableMap<T> :IDapperStatementToDataTableMap<T> where T: class
    {
        private readonly Dictionary<string, Func<T, object>> _mapping;
        private readonly DataTable _table;
        private long _uniqueId = 1;

        internal DapperStatementToDataTableMap()
        {
            _mapping = new Dictionary<string, Func<T, object>>();
            _table = new DataTable();
        }

        private DataColumn NewColumn<V>(string colName, Func<T, V> getVal)
        {
            var dbType = typeof(V);
            var nonNullableType = Nullable.GetUnderlyingType(dbType);
            var trueType = nonNullableType ?? dbType;

            var col = new DataColumn(colName, trueType) {AllowDBNull = nonNullableType != null};
            _table.Columns.Add(col);

            if (trueType == typeof(bool))
            {
                if (col.AllowDBNull)
                    _mapping[colName] = x =>
                    {
                        var val = getVal(x);
                        if (val != null)
                            return Convert.ToBoolean(val) ? 1 : 0;
                        return null;
                    };
                else
                    _mapping[colName] = x => Convert.ToBoolean(getVal(x)) ? 1 : 0;
            }
            else
                _mapping[colName] = x => getVal(x);

            return col;
        }

        private DataColumn NewStringColumn(string colName, int maxLength, Func<T, string> getVal)
        {
            var col = new DataColumn(colName, typeof(string))
            {
                AllowDBNull = false,
                MaxLength = maxLength
            };
            _table.Columns.Add(col);
            
            _mapping[colName] = x =>
            {
                var val = getVal(x);
                return (val ?? "").Truncate(maxLength);
            };
            return col;
        }

        public IDapperStatementToDataTableMap<T> String(string colName, int maxLength, Func<T, string> getVal) =>
            this.Fluent(x => NewStringColumn(colName, maxLength, getVal));

        public IDapperStatementToDataTableMap<T> Map<V>(string colName, Func<T, V> getVal) =>
            this.Fluent(x => NewColumn(colName, getVal));
        
        public IDapperStatementToDataTableMap<T> Primary<V>(string colName, Func<T, V> getVal)
        {
            var col = NewColumn(colName, getVal);
            _table.PrimaryKey = _table.PrimaryKey.Append(col).ToArray();
            return this;
        }
        public IDapperStatementToDataTableMap<T> Primary(string colName) => Primary(colName, d => _uniqueId++);
        public IDapperStatementToDataTableMap<T> Primary(string colName, int maxLength, Func<T, string> getVal)
        {
            var col = NewStringColumn(colName, maxLength, getVal);
            _table.PrimaryKey = _table.PrimaryKey.Append(col).ToArray();
            return this;
        }

        public DataTable Fill(IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                var row = _table.NewRow();
                foreach (var colName in _mapping.Keys)
                {
                    var val = _mapping[colName](item);
                    row[colName] = val ?? DBNull.Value;
                }
                _table.Rows.Add(row);
            }

            return _table;
        }
    }
}