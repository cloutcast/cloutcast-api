using System.Linq;

namespace CloutCast
{
    public class TableName
    {
        public TableName(string name) => Name = name;
        public string Name { get; }

        public string ToReferenceCol(string suffix = "Id")
        {
            if (Name.EndsWith("User")) return $"User{suffix}";
            if (Name.EndsWith("es")) return $"{Name.RemoveFromEnd(2)}{suffix}";
            if (Name.EndsWith("s")) return $"{Name.RemoveFromEnd()}{suffix}";
            return $"{Name}{suffix}";
        }

        public string ToForeignKeyName(string column, TableName dest) => $"FK_{Name}_{column}_{dest.Name}_Id";
        public string ToForeignKeyName(TableName dest) => $"FK_{Name}_{dest.ToReferenceCol()}_{dest.Name}_Id";
        
        public string ToIndexName(params string[] columns) => columns.Aggregate($"IX_{Name}", (current, column) => current + $"_{column}");
        public string ToUniqueIndexName(params string[] columns) => columns.Aggregate($"UX_{Name}", (current, column) => current + $"_{column}");

        public override string ToString() => Name;
    }
}