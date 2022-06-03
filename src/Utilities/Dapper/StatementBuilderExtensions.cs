using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using Dapper;

namespace CloutCast
{
    public static class StatementBuilderExtensions
    {
        public static string ToDebug(this IStatementBuilderOutput output)
        {
            var sb = new StringBuilder();
            var declarations = new StringBuilder();
            var setters = new StringBuilder();
            var sql = new StringBuilder(output.Sql);

            if ((output.Parameters as ExpandoObject) is IDictionary<string, object> qp)
            {
                foreach (var par in qp.Select(ParamToDebug))
                {
                    if (par.StartsWith("REPLACE"))
                    {
                        var p = par.Replace("REPLACE", "").Split('=');
                        sql = sql.Replace(p[0], p[1]);
                    }
                    else
                    {
                        var p = par.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
                        declarations.AppendLine(p[0]);
                        if (p.Length <= 1) 
                            continue;
                        if (p[1].ToUpper().StartsWith("INSERT"))
                            p.Skip(1).ForEach(ln => setters.AppendLine(ln));
                        else
                            setters.AppendLine(p[1]);
                    }
                }
            }

            sb.Append(declarations).Append(setters);
            if (sb.Length > 0) sb.AppendLine();
            sb.Append(sql);

            return sb.ToString();
        }

        private static bool IsTableType(Type sourceType) =>
            typeof(SqlMapper.ICustomQueryParameter).IsAssignableFrom(sourceType) &&
            sourceType.FullName == "Dapper.TableValuedParameter";

        private static string ParamToDebug(KeyValuePair<string, object> kvp)
        {
            var val = kvp.Value;
            var key = kvp.Key;

            if (val == null)
                return $@"DECLARE @{key} AS nvarchar(max) 
SET @{key} = NULL 
";
            var sourceType = val.GetType();
            var innerType = sourceType.IsArray ? sourceType.GetElementType() : sourceType.GetListType();
            if (innerType != null)
            {
                var enumeration = ((IEnumerable) val).Cast<object>().ToList();
                if (innerType == typeof (byte))
                    return $"REPLACE @{key}=BYTE[{enumeration.Count}]";

                var listVal = enumeration.ToCommaDelimitedString(false, v => ValToDbSafeVal(innerType, v));
                return $"REPLACE @{key}= ({listVal})";
            }
            var dbType = TypeToDbType(sourceType, val);
            if (IsTableType(sourceType))
            {
                var typeNameField = sourceType.GetField("table", BindingFlags.Instance | BindingFlags.NonPublic);
                if (typeNameField?.GetValue(val) is DataTable dt)
                    return $"DECLARE @{key} AS {dbType}\r\n{ToInsert(dt, $"@{key}")}";
            }
            else if (string.IsNullOrEmpty(dbType))
                return $"-- Unknown! [Type = {sourceType.Name} ]";

            return $"DECLARE @{key} AS {dbType}\r\nSET @{key} = {ValToDbSafeVal(sourceType, val)} \r\n";
        }

        private static string ToInsert(DataTable source, string tableName)
        {
            if (source == null) return "";
            if (source.Rows.Count == 0) return "";

            var columns = string.Join(", ", source.Columns.Cast<DataColumn>().Select(c => c.ColumnName));

            var values = new StringBuilder();
            foreach (DataRow row in source.Rows)
            {
                values.AppendLine().Append("  (");
                foreach (DataColumn column in source.Columns)
                {
                    var val = row[column];
                    if (column.DataType == typeof(string))
                        values.Append($"'{val}', ");
                    
                    else if (column.DataType == typeof(bool))
                        values.Append($"{((bool) val ? 1 : 0)}, ");

                    else if ((column.AllowDBNull && val == null) || val is DBNull dbNullVal && dbNullVal == DBNull.Value)
                        values.Append("null, ");
                    
                    else if (column.DataType == typeof(DateTimeOffset))
                        values.Append($"'{val}', ");
                    
                    else
                        values.Append($"{val}, ");

                }

                values.Length -= 2;
                values.Append("),");
            }

            if (values.Length > 1)
                values.Length -= 1;

            return $@"
INSERT INTO {tableName} ({columns}) 
VALUES {values}";
        }

        private static string TypeToDbType(Type sourceType, object source)
        {
            if (sourceType == typeof(string)) return "nvarchar(max)";
            if (sourceType == typeof(long) || sourceType == typeof(ulong) || sourceType == typeof(uint)) return "bigint";
            if (sourceType.IsEnum || sourceType == typeof(int) || sourceType == typeof(ushort)) return "int";
            if (sourceType == typeof(short) || sourceType == typeof(byte)) return "smallint";
            if (sourceType == typeof(DateTimeOffset)) return "datetimeoffset(7)";
            if (sourceType == typeof(DateTime)) return "datetime";
            if (sourceType == typeof(bool)) return "bit";
            if (sourceType == typeof(decimal)) return "numeric(19,5)";
            if (sourceType == typeof(double)) return "decimal(10,2)";
            if (sourceType == typeof(Guid)) return "uniqueidentifier";

            if (IsTableType(sourceType)) 
            {
                var typeNameField = sourceType.GetField("typeName", BindingFlags.Instance | BindingFlags.NonPublic);
                return (typeNameField?.GetValue(source) ?? "").ToString();
            }

            return "";
        }

        private static string ValToDbSafeVal(Type sourceType, object val)
        {
            if (sourceType == typeof(string)) return $"'{val}'";
            if (sourceType == typeof(long)) return $"{(long) val}";
            if (sourceType == typeof(ulong)) return $"{(ulong) val}";
            if (sourceType.IsEnum || sourceType == typeof(int)) return $"{(int) val}";
            if (sourceType == typeof(ushort) || sourceType == typeof(uint) )
                return $"{(int) val}";

            if (sourceType == typeof(short) || sourceType == typeof(byte)) return $"{(short) val}";

            if (sourceType == typeof(DateTimeOffset))
                return $"ToDateTimeOffset('{(DateTimeOffset) val:yyyy-MM-dd HH:mm:ss.fff}', '{(DateTimeOffset) val:zzz}')";

            if (sourceType == typeof(DateTime)) return $"'{(DateTime)val:yyyy-MM-dd HH:mm:ss}'";
            if (sourceType == typeof(bool)) return (bool)val ? "1" : "0";
            if (sourceType == typeof(decimal)) return $"{(decimal) val:0.#####}";
            if (sourceType == typeof(double)) return $"{(double) val:0.##}";

            return sourceType == typeof(Guid) ? $"'{(Guid) val}'" : "";
        }

    }
}