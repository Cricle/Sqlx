using Xunit;
using Sqlx;
using System;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for SqlDialect and SqlDefine classes
    /// </summary>
    public class SqlDialect_Tests
    {
        [Fact]
        public void SqlServer_HasCorrectDelimiters()
        {
            var dialect = SqlDefine.SqlServer;
            
            Assert.Equal("[", dialect.ColumnLeft);
            Assert.Equal("]", dialect.ColumnRight);
            Assert.Equal("@", dialect.ParameterPrefix);
        }

        [Fact]
        public void MySql_HasCorrectDelimiters()
        {
            var dialect = SqlDefine.MySql;
            
            Assert.Equal("`", dialect.ColumnLeft);
            Assert.Equal("`", dialect.ColumnRight);
            Assert.Equal("@", dialect.ParameterPrefix);
        }

        [Fact]
        public void PostgreSql_HasCorrectDelimiters()
        {
            var dialect = SqlDefine.PostgreSql;
            
            Assert.Equal("\"", dialect.ColumnLeft);
            Assert.Equal("\"", dialect.ColumnRight);
            Assert.Equal("$", dialect.ParameterPrefix);
        }

        [Fact]
        public void Oracle_HasCorrectDelimiters()
        {
            var dialect = SqlDefine.Oracle;
            
            Assert.Equal("\"", dialect.ColumnLeft);
            Assert.Equal("\"", dialect.ColumnRight);
            Assert.Equal(":", dialect.ParameterPrefix);
        }

        [Fact]
        public void DB2_HasCorrectDelimiters()
        {
            var dialect = SqlDefine.DB2;
            
            Assert.Equal("\"", dialect.ColumnLeft);
            Assert.Equal("\"", dialect.ColumnRight);
            Assert.Equal("?", dialect.ParameterPrefix);
        }

        [Fact]
        public void SQLite_HasCorrectDelimiters()
        {
            var dialect = SqlDefine.SQLite;
            
            Assert.Equal("[", dialect.ColumnLeft);
            Assert.Equal("]", dialect.ColumnRight);
            Assert.Equal("@", dialect.ParameterPrefix);
        }

        [Fact]
        public void PgSql_IsAliasForPostgreSql()
        {
            Assert.Equal(SqlDefine.PostgreSql, SqlDefine.PgSql);
        }

        [Fact]
        public void Sqlite_IsAliasForSQLite()
        {
            Assert.Equal(SqlDefine.SQLite, SqlDefine.Sqlite);
        }

        [Fact]
        public void WrapColumn_WrapsColumnName()
        {
            var dialect = SqlDefine.SqlServer;
            var wrapped = dialect.WrapColumn("UserId");
            
            Assert.Equal("[UserId]", wrapped);
        }

        [Fact]
        public void WrapColumn_EmptyString_ReturnsEmpty()
        {
            var dialect = SqlDefine.SqlServer;
            var wrapped = dialect.WrapColumn("");
            
            Assert.Equal("", wrapped);
        }

        [Fact]
        public void WrapColumn_Null_ReturnsEmpty()
        {
            var dialect = SqlDefine.SqlServer;
            var wrapped = dialect.WrapColumn(null!);
            
            Assert.Equal("", wrapped);
        }

        [Fact]
        public void WrapString_WrapsStringValue()
        {
            var dialect = SqlDefine.SqlServer;
            var wrapped = dialect.WrapString("test");
            
            Assert.Equal("'test'", wrapped);
        }

        [Fact]
        public void WrapString_Null_ReturnsNull()
        {
            var dialect = SqlDefine.SqlServer;
            var wrapped = dialect.WrapString(null!);
            
            Assert.Equal("NULL", wrapped);
        }

        [Fact]
        public void WrapString_EscapesQuotes()
        {
            var dialect = SqlDefine.SqlServer;
            var wrapped = dialect.WrapString("O'Brien");
            
            Assert.Equal("'O''Brien'", wrapped);
        }

        [Fact]
        public void CreateParameter_CreatesParameterWithPrefix()
        {
            var dialect = SqlDefine.SqlServer;
            var param = dialect.CreateParameter("id");
            
            Assert.Equal("@id", param);
        }

        [Fact]
        public void CreateParameter_PostgreSql_UsesDollarSign()
        {
            var dialect = SqlDefine.PostgreSql;
            var param = dialect.CreateParameter("id");
            
            Assert.Equal("$id", param);
        }

        [Fact]
        public void CreateParameter_Oracle_UsesColon()
        {
            var dialect = SqlDefine.Oracle;
            var param = dialect.CreateParameter("id");
            
            Assert.Equal(":id", param);
        }

        [Fact]
        public void DatabaseType_SqlServer_ReturnsSqlServer()
        {
            var dialect = SqlDefine.SqlServer;
            
            Assert.Equal("SqlServer", dialect.DatabaseType);
        }

        [Fact]
        public void DatabaseType_MySql_ReturnsMySql()
        {
            var dialect = SqlDefine.MySql;
            
            Assert.Equal("MySql", dialect.DatabaseType);
        }

        [Fact]
        public void DatabaseType_PostgreSql_ReturnsPostgreSql()
        {
            var dialect = SqlDefine.PostgreSql;
            
            Assert.Equal("PostgreSql", dialect.DatabaseType);
        }

        [Fact]
        public void DatabaseType_Oracle_ReturnsOracle()
        {
            var dialect = SqlDefine.Oracle;
            
            Assert.Equal("Oracle", dialect.DatabaseType);
        }

        [Fact]
        public void DatabaseType_DB2_ReturnsDB2()
        {
            var dialect = SqlDefine.DB2;
            
            Assert.Equal("DB2", dialect.DatabaseType);
        }

        [Fact]
        public void DbType_SqlServer_ReturnsSqlServerEnum()
        {
            var dialect = SqlDefine.SqlServer;
            
            Assert.Equal(Annotations.SqlDefineTypes.SqlServer, dialect.DbType);
        }

        [Fact]
        public void DbType_MySql_ReturnsMySqlEnum()
        {
            var dialect = SqlDefine.MySql;
            
            Assert.Equal(Annotations.SqlDefineTypes.MySql, dialect.DbType);
        }

        [Fact]
        public void GetConcatFunction_SqlServer_UsesPlus()
        {
            var dialect = SqlDefine.SqlServer;
            var concat = dialect.GetConcatFunction("'Hello'", "' '", "'World'");
            
            Assert.Equal("'Hello' + ' ' + 'World'", concat);
        }

        [Fact]
        public void GetConcatFunction_PostgreSql_UsesPipes()
        {
            var dialect = SqlDefine.PostgreSql;
            var concat = dialect.GetConcatFunction("'Hello'", "' '", "'World'");
            
            Assert.Equal("'Hello' || ' ' || 'World'", concat);
        }

        [Fact]
        public void GetConcatFunction_Oracle_UsesPipes()
        {
            var dialect = SqlDefine.Oracle;
            var concat = dialect.GetConcatFunction("'Hello'", "' '", "'World'");
            
            Assert.Equal("'Hello' || ' ' || 'World'", concat);
        }

        [Fact]
        public void GetConcatFunction_MySql_UsesConcat()
        {
            var dialect = SqlDefine.MySql;
            var concat = dialect.GetConcatFunction("'Hello'", "' '", "'World'");
            
            Assert.Equal("CONCAT('Hello', ' ', 'World')", concat);
        }

        [Fact]
        public void GetDialect_MySql_ReturnsMySqlDialect()
        {
            var dialect = SqlDefine.GetDialect(Annotations.SqlDefineTypes.MySql);
            
            Assert.Equal(SqlDefine.MySql, dialect);
        }

        [Fact]
        public void GetDialect_SqlServer_ReturnsSqlServerDialect()
        {
            var dialect = SqlDefine.GetDialect(Annotations.SqlDefineTypes.SqlServer);
            
            Assert.Equal(SqlDefine.SqlServer, dialect);
        }

        [Fact]
        public void GetDialect_PostgreSql_ReturnsPostgreSqlDialect()
        {
            var dialect = SqlDefine.GetDialect(Annotations.SqlDefineTypes.PostgreSql);
            
            Assert.Equal(SqlDefine.PostgreSql, dialect);
        }

        [Fact]
        public void GetDialect_SQLite_ReturnsSQLiteDialect()
        {
            var dialect = SqlDefine.GetDialect(Annotations.SqlDefineTypes.SQLite);
            
            Assert.Equal(SqlDefine.SQLite, dialect);
        }

        [Fact]
        public void GetDialect_Oracle_ReturnsOracleDialect()
        {
            var dialect = SqlDefine.GetDialect(Annotations.SqlDefineTypes.Oracle);
            
            Assert.Equal(SqlDefine.Oracle, dialect);
        }

        [Fact]
        public void GetDialect_DB2_ReturnsDB2Dialect()
        {
            var dialect = SqlDefine.GetDialect(Annotations.SqlDefineTypes.DB2);
            
            Assert.Equal(SqlDefine.DB2, dialect);
        }

        [Fact]
        public void GetDialect_InvalidType_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => 
                SqlDefine.GetDialect((Annotations.SqlDefineTypes)999));
        }

        [Fact]
        public void CustomDialect_CanBeCreated()
        {
            var customDialect = new SqlDialect("<", ">", "\"", "\"", "#");
            
            Assert.Equal("<", customDialect.ColumnLeft);
            Assert.Equal(">", customDialect.ColumnRight);
            Assert.Equal("#", customDialect.ParameterPrefix);
        }

        [Fact]
        public void CustomDialect_WrapColumn_UsesCustomDelimiters()
        {
            var customDialect = new SqlDialect("<", ">", "\"", "\"", "#");
            var wrapped = customDialect.WrapColumn("UserId");
            
            Assert.Equal("<UserId>", wrapped);
        }
    }
}
