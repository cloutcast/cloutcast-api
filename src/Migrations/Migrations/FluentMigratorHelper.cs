using System;
using FluentMigrator;
using FluentMigrator.Builders;
using FluentMigrator.Builders.Alter;
using FluentMigrator.Builders.Alter.Column;
using FluentMigrator.Builders.Alter.Table;
using FluentMigrator.Builders.Create;
using FluentMigrator.Builders.Create.ForeignKey;
using FluentMigrator.Builders.Create.Index;
using FluentMigrator.Builders.Create.Table;
using FluentMigrator.Builders.Delete;
using FluentMigrator.Builders.Delete.Column;
using FluentMigrator.Builders.Delete.ForeignKey;
using FluentMigrator.Builders.Delete.Index;
using FluentMigrator.Builders.Rename;
using FluentMigrator.Builders.Rename.Column;
using FluentMigrator.Builders.Rename.Table;
using FluentMigrator.Builders.Schema.Schema;
using FluentMigrator.Builders.Schema.Table;
using FluentMigrator.Infrastructure;

namespace CloutCast.Migrations
{
    public static class FluentMigratorHelper
    {
        internal static ICreateIndexOnColumnOrInSchemaSyntax OnTable(this ICreateIndexForTableSyntax root, TableName tableName) => root.OnTable(tableName.Name);
        internal static ICreateTableWithColumnOrSchemaOrDescriptionSyntax Table(this ICreateExpressionRoot root, TableName tableName) => root.Table(tableName.Name);
        internal static ICreateForeignKeyForeignColumnOrInSchemaSyntax FromTable(this ICreateForeignKeyFromTableSyntax root, TableName tableName) => root.FromTable(tableName.Name);
        internal static IRenameTableToOrInSchemaSyntax Table(this IRenameExpressionRoot root, TableName old) => root.Table(old.Name);
        internal static IInSchemaSyntax To(this IRenameTableToSyntax root, TableName to) => root.To(to.Name);

        internal static ICreateForeignKeyPrimaryColumnOrInSchemaSyntax ToTable(this ICreateForeignKeyToTableSyntax root, TableName tableName) => root.ToTable(tableName.Name);
        internal static IInSchemaSyntax OnTable(this IDeleteForeignKeyOnTableSyntax root, TableName foreignTableName) => root.OnTable(foreignTableName.Name);
        internal static IDeleteIndexOnColumnOrInSchemaSyntax OnTable(this IDeleteIndexForTableSyntax root, TableName tableName) => root.OnTable(tableName.Name);
        internal static IInSchemaSyntax Table(this IDeleteExpressionRoot root, TableName tableName) => root.Table(tableName.Name);
        internal static IInSchemaSyntax FromTable(this IDeleteColumnFromTableSyntax root, TableName tableName) => root.FromTable(tableName.Name);
        internal static IAlterTableAddColumnOrAlterColumnOrSchemaOrDescriptionSyntax Table(this IAlterExpressionRoot root, TableName tableName) => root.Table(tableName.Name);
        internal static IRenameColumnToOrInSchemaSyntax OnTable(this IRenameColumnTableSyntax root, TableName tableName) => root.OnTable(tableName.Name);
        internal static IAlterColumnAsTypeOrInSchemaSyntax OnTable(this IAlterColumnOnTableSyntax root, TableName tableName) => root.OnTable(tableName.Name);
        internal static ISchemaTableSyntax Table(this ISchemaSchemaSyntax root, TableName tableName) => root.Table(tableName.Name);

        internal static M CreateForeignKey<M>(this M self,
            TableName currentTable,
            TableName foreignTable,
            string refColumnToForeignTable = "",
            string foreignTableId = "Id") where M : MigrationBase
        {
            if (refColumnToForeignTable.IsEmpty()) refColumnToForeignTable = foreignTable.ToReferenceCol();
            var fkName = currentTable.ToForeignKeyName(refColumnToForeignTable, foreignTable);

            self.Create.ForeignKey(fkName)
                .FromTable(currentTable).ForeignColumn(refColumnToForeignTable)
                .ToTable(foreignTable).PrimaryColumn(foreignTableId);

            return self;
        }

        internal static M DeleteForeignKey<M>(this M self,
            TableName currentTable,
            TableName foreignTable,
            string refColumnToForeignTable = "") where M: Migration
        {
            if (refColumnToForeignTable.IsEmpty()) refColumnToForeignTable = foreignTable.ToReferenceCol();
            var fkName = currentTable.ToForeignKeyName(refColumnToForeignTable, foreignTable);

            self.Delete.ForeignKey(fkName).OnTable(currentTable);
            return self;
        }

        public static M CreateUserTableType<M>(this M source, string name, string contents) where M :Migration
        {
            DropUserTableType(source, name);
            source.Execute.Sql($@"
CREATE TYPE [dbo].{name} AS TABLE 
(
{contents.TrimEnd()}
)
GO

GRANT EXEC ON TYPE::[dbo].[{name}] TO PUBLIC");
            return source;
        }

        public static M DropUserTableType<M>(this M source, string name) where M: Migration
        {
            source.Execute.Sql($@"
IF TYPE_ID('dbo.{name}') IS NOT NULL 
  DROP TYPE [dbo].[{name}]".Trim());
            return source;
        }

        public static void CreateIndexIfNotExists(this MigrationBase self, TableName table, string indexName, Action<ICreateIndexOnColumnSyntax> indexFunc) => 
            CreateIndexIfNotExists(self, table.ToString(), indexName, indexFunc);

        public static void CreateIndexIfNotExists(this MigrationBase self, string tableName, string indexName, Action<ICreateIndexOnColumnSyntax> indexFunc)
        {
            if (indexFunc!= null && !self.Schema.Table(tableName).Index(indexName).Exists())
                indexFunc(self.Create.Index(indexName).OnTable(tableName));
        }

        internal static void DeleteForeignKeyIfExists<M>(this M self, TableName tableName, string foreignKey)
            where M : Migration => DeleteForeignKeyIfExists(self, tableName.Name, foreignKey);

        public static void DeleteForeignKeyIfExists<M>(this M self, string tableName, string foreignKey) where M: Migration
        {
            if (self.Schema.Table(tableName).Constraint(foreignKey).Exists())
                self.Delete.ForeignKey(foreignKey).OnTable(tableName);
        }

        internal static void DeleteIndexIfExists(this Migration self, TableName tableName, string indexName) =>
            DeleteIndexIfExists(self, tableName.Name, indexName);

        public static void DeleteIndexIfExists(this Migration self, string tableName, string indexName)
        {
            if (self.Schema.Table(tableName).Index(indexName).Exists())
                self.Delete.Index(indexName).OnTable(tableName);
        }

        internal static Migration DeleteIndex(this Migration self, TableName source, params string[] columns)
        {
            var indexName = source.ToIndexName(columns);
            DeleteIndexIfExists(self, source, indexName);
            return self;
        }

        internal static IFluentSyntax CreateTableIfNotExists(this MigrationBase self, TableName source,
            Func<ICreateTableWithColumnOrSchemaOrDescriptionSyntax, IFluentSyntax> constructTableFunction,
            string schemaName = "dbo") =>
            self.Schema.Schema(schemaName).Table(source).Exists()
                ? null
                : constructTableFunction(self.Create.Table(source));

        internal static bool DoesColumnExist(this MigrationBase self, TableName source, string columnName,
            string schemaName = "dbo")
            => self.Schema.Schema("dbo").Table(source).Column("POType").Exists();

        public static IFluentSyntax CreateTableIfNotExists(this MigrationBase self, string tableName,
            Func<ICreateTableWithColumnOrSchemaOrDescriptionSyntax, IFluentSyntax> constructTableFunction,
            string schemaName = "dbo") =>
            self.Schema.Schema(schemaName).Table(tableName).Exists()
                ? null
                : constructTableFunction(self.Create.Table(tableName));

        public static Migration AddConstraint(this Migration migration, TableName source, string checkFunction, string checkName = null, string schemaName = "dbo")
            => AddConstraint(migration, source.Name, checkFunction, checkName, schemaName);

        public static Migration AddConstraint(this Migration migration, string tableName, string checkFunction, string checkName = null, string schemaName = "dbo")
        {
            if (migration == null) throw new ArgumentNullException(nameof(migration));
            if (string.IsNullOrWhiteSpace(schemaName)) throw new ArgumentException(nameof(schemaName));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(checkName)) checkName = $"CK_{tableName}";
            if (string.IsNullOrWhiteSpace(checkFunction)) throw new ArgumentException(nameof(checkFunction));
            migration.Execute.Sql($@"
ALTER TABLE  [{schemaName}].[{tableName}]
    ADD CONSTRAINT [{checkName}]
                   CHECK ({checkFunction})".Trim());
            return migration;
        }

        public static Migration DeleteConstraint(this Migration migration, TableName source, string checkName = null, string schemaName = "dbo")
            => DeleteConstraint(migration, source.Name, checkName, schemaName);
        public static Migration DeleteConstraint(this Migration migration, string tableName, string checkName = null, string schemaName = "dbo")
        {
            if (migration == null) throw new ArgumentNullException(nameof(migration));
            if (string.IsNullOrWhiteSpace(schemaName)) throw new ArgumentException(nameof(schemaName));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(checkName)) checkName = $"CK_{tableName}";
            migration.Execute.Sql($"ALTER TABLE  [{schemaName}].[{tableName}] DROP CONSTRAINT [{checkName}]".Trim());
            return migration;
        }
    }
}