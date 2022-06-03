using System;
using System.Data.SqlClient;
using System.Linq;
using Dapper;

namespace CloutCast
{
    using Entities;

    public partial class WebAppFixture
    {
        public DBUtils Database { get; } = new DBUtils();

        public class DBUtils
        {
            public void EnsureDatabase(string name)
            {
                var parameters = new DynamicParameters();

                parameters.Add("name", name);
                using var connection = OpenConnection();
                var records = connection.Query("SELECT * FROM sys.databases WHERE name = @name", parameters);
                if (!records.Any())
                    connection.Execute($"CREATE DATABASE {name}");
            }

            public SqlConnection OpenConnection()
            {
                SqlConnection connection;
                lock (new object())
                {
                    connection = new SqlConnection(ConnString());
                    connection.Open();
                }
                return connection;
            }

            public void Reset()
            {
                using var conn = OpenConnection();
                const string RESET_STATEMENTS = @"
DECLARE @sql NVARCHAR(2000)

WHILE(EXISTS(SELECT 1 from INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_TYPE='FOREIGN KEY'))
BEGIN
    SELECT TOP 1 @sql=('ALTER TABLE ' + TABLE_SCHEMA + '.[' + TABLE_NAME + '] DROP CONSTRAINT [' + CONSTRAINT_NAME + ']')
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_TYPE = 'FOREIGN KEY'
    EXEC(@sql)
    PRINT @sql
END

WHILE(EXISTS(SELECT * from INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'VIEW' AND TABLE_NAME != 'database_firewall_rules'))
BEGIN
    SELECT TOP 1 @sql=('DROP VIEW ' + TABLE_SCHEMA + '.[' + TABLE_NAME + ']')
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_TYPE = 'VIEW'
    AND   TABLE_NAME != 'database_firewall_rules'
    EXEC(@sql)
    PRINT @sql
END

WHILE(EXISTS(SELECT * from INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND  TABLE_NAME != '__MigrationHistory'))
BEGIN
    SELECT TOP 1 @sql=('DROP TABLE ' + TABLE_SCHEMA + '.[' + TABLE_NAME + ']')
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_TYPE = 'BASE TABLE'
    AND   TABLE_NAME != '__MigrationHistory'     
    EXEC(@sql)
    PRINT @sql
END";
                conn.Execute(RESET_STATEMENTS);
            }

            public void UseConnection(Action<SqlConnection> action)
            {
                using var c = OpenConnection();
                action(c);
                c.Close();
            }
        }
    }

    public static class SqlConnectionExtensions
    {
        public static BitCloutUser AddAndGetUser(this SqlConnection conn, string handle, string publicKey) => conn
            .AddUser(handle, publicKey)
            .GetUser(publicKey);

        public static AppSource App(this SqlConnection conn, string name) => conn
            .Query<AppSource>($"select id, apiKey, company, name from {Tables.App} where name = '{name}'")
            .SingleOrDefault();

        public static SqlConnection AddUser(this SqlConnection conn, string handle, string publicKey)
        {
            conn.Execute($@"
IF NOT EXISTS (SELECT Id from {Tables.User} where PublicKey = '{publicKey}') BEGIN

    DECLARE @UserId as bigint
    INSERT INTO {Tables.User} (Handle, PublicKey) VALUES ('{handle}', '{publicKey}');
    SET @UserId = SCOPE_IDENTITY()
    
    INSERT INTO {Tables.EntityLog} (Timestamp, Action, AppId, UserId)
    SELECT GETUTCDATE(), {(int) EntityAction.UserRegister}, ap.Id, @UserId
    FROM {Tables.App} ap
    WHERE ap.Name = 'CloutCast'
    
    INSERT INTO {Tables.GeneralLedgerAccount} (LedgerTypeId, UserId)
    SELECT glt.Id, u.Id
    FROM {Tables.GeneralLedgerType} glt
    CROSS JOIN {Tables.User} u
    LEFT OUTER JOIN {Tables.GeneralLedgerAccount} gla on gla.UserId = u.Id and gla.LedgerTypeId = glt.Id
    WHERE u.Id = @UserId AND gla.Id IS NULL
END");
            return conn;
        }

        public static SqlConnection CleanUp(this SqlConnection conn)
        {
            conn.Execute($@"
DELETE FROM {Tables.Evidence}
DELETE FROM {Tables.GeneralLedger}
DELETE FROM {Tables.EntityLog}

DELETE FROM {Tables.GeneralLedgerAccount} WHERE PromotionId IS NOT NULL
DELETE FROM {Tables.GeneralLedgerAccount} WHERE UserId <> 1

DELETE FROM {Tables.PromotionUsers}
DELETE FROM {Tables.Promotion}


DELETE FROM {Tables.User} WHERE Id > 1
");

            return conn;
        }

        public static BitCloutUser GetUser(this SqlConnection conn, string publicKey) => conn
            .Query<BitCloutUser>($"select id, handle, publicKey from {Tables.User} where publicKey = @PublicKey", new {PublicKey = publicKey})
            .SingleOrDefault();
    }
}