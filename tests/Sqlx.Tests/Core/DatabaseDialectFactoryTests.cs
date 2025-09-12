using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using Sqlx.SqlGen;
using System;

namespace Sqlx.Tests.Core;

[TestClass]
public class DatabaseDialectFactoryTests
{
    [TestMethod]
    public void GetDialectProvider_WithMySqlDefine_ReturnsMySqlProvider()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;

        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(sqlDefine);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(MySqlDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_WithSqlServerDefine_ReturnsSqlServerProvider()
    {
        // Arrange
        var sqlDefine = SqlDefine.SqlServer;

        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(sqlDefine);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_WithPostgreSqlDefine_ReturnsPostgreSqlProvider()
    {
        // Arrange
        var sqlDefine = SqlDefine.PgSql;

        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(sqlDefine);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(PostgreSqlDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_WithSQLiteDefine_ReturnsSQLiteProvider()
    {
        // Arrange
        var sqlDefine = SqlDefine.SQLite;

        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(sqlDefine);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(SQLiteDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_WithSqlDefineTypes_MySql_ReturnsMySqlProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.MySql);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(MySqlDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_WithSqlDefineTypes_SqlServer_ReturnsSqlServerProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.SqlServer);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_WithSqlDefineTypes_PostgreSql_ReturnsPostgreSqlProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.Postgresql);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(PostgreSqlDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_WithSqlDefineTypes_SQLite_ReturnsSQLiteProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.SQLite);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(SQLiteDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_WithOracleDefineTypes_ThrowsNotSupportedException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<UnsupportedDialectException>(() =>
            DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.Oracle));

        Assert.IsTrue(exception.Message.Contains("Oracle"));
    }

    [TestMethod]
    public void GetDialectProvider_WithDB2DefineTypes_ThrowsNotSupportedException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<UnsupportedDialectException>(() =>
            DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.DB2));

        Assert.IsTrue(exception.Message.Contains("DB2"));
    }

    [TestMethod]
    public void GetDialectProvider_WithOracleDefine_ThrowsNotSupportedException()
    {
        // Arrange
        var sqlDefine = SqlDefine.Oracle;

        // Act & Assert
        var exception = Assert.ThrowsException<UnsupportedDialectException>(() =>
            DatabaseDialectFactory.GetDialectProvider(sqlDefine));

        Assert.IsTrue(exception.Message.Contains("Oracle"));
    }

    [TestMethod]
    public void GetDialectProvider_WithDB2Define_ThrowsNotSupportedException()
    {
        // Arrange
        var sqlDefine = SqlDefine.DB2;

        // Act & Assert
        var exception = Assert.ThrowsException<UnsupportedDialectException>(() =>
            DatabaseDialectFactory.GetDialectProvider(sqlDefine));

        Assert.IsTrue(exception.Message.Contains("DB2"));
    }

    [TestMethod]
    public void MySqlDialectProvider_SqlDefine_HasCorrectValues()
    {
        // Arrange
        var provider = new MySqlDialectProvider();

        // Act
        var sqlDefine = provider.SqlDefine;

        // Assert
        Assert.AreEqual("`", sqlDefine.ColumnLeft);
        Assert.AreEqual("`", sqlDefine.ColumnRight);
        Assert.AreEqual("@", sqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlServerDialectProvider_SqlDefine_HasCorrectValues()
    {
        // Arrange
        var provider = new SqlServerDialectProvider();

        // Act
        var sqlDefine = provider.SqlDefine;

        // Assert
        Assert.AreEqual("[", sqlDefine.ColumnLeft);
        Assert.AreEqual("]", sqlDefine.ColumnRight);
        Assert.AreEqual("@", sqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void PostgreSqlDialectProvider_SqlDefine_HasCorrectValues()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();

        // Act
        var sqlDefine = provider.SqlDefine;

        // Assert
        Assert.AreEqual("\"", sqlDefine.ColumnLeft);
        Assert.AreEqual("\"", sqlDefine.ColumnRight);
        Assert.AreEqual("$", sqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SQLiteDialectProvider_SqlDefine_HasCorrectValues()
    {
        // Arrange
        var provider = new SQLiteDialectProvider();

        // Act
        var sqlDefine = provider.SqlDefine;

        // Assert
        Assert.AreEqual("[", sqlDefine.ColumnLeft);
        Assert.AreEqual("]", sqlDefine.ColumnRight);
        Assert.AreEqual("@", sqlDefine.ParameterPrefix); // SQLiteDialectProvider uses @ for actual parameters
    }

    [TestMethod]
    public void MySqlDialectProvider_GenerateLimitClause_ReturnsCorrectSyntax()
    {
        // Arrange
        var provider = new MySqlDialectProvider();

        // Act
        var result = provider.GenerateLimitClause(10, 5);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("LIMIT"));
    }

    [TestMethod]
    public void SqlServerDialectProvider_GenerateInsertWithReturning_ReturnsCorrectSyntax()
    {
        // Arrange
        var provider = new SqlServerDialectProvider();

        // Act
        var result = provider.GenerateInsertWithReturning("users", new[] { "Name", "Email" });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("INSERT"));
        Assert.IsTrue(result.Contains("users"));
    }

    [TestMethod]
    public void PostgreSqlDialectProvider_GetCurrentDateTimeSyntax_ReturnsCorrectValue()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();

        // Act
        var result = provider.GetCurrentDateTimeSyntax();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result));
    }

    [TestMethod]
    public void AllDialectProviders_HaveConsistentInterface()
    {
        // Arrange
        var providers = new IDatabaseDialectProvider[]
        {
            new MySqlDialectProvider(),
            new SqlServerDialectProvider(),
            new PostgreSqlDialectProvider(),
            new SQLiteDialectProvider()
        };

        // Act & Assert
        foreach (var provider in providers)
        {
            Assert.IsNotNull(provider.WrapColumn("test"));
            Assert.IsNotNull(provider.WrapString("test"));
            Assert.IsNotNull(provider.GetParameterPrefix());

            // Ensure wrapped results are not empty and contain the original text
            var wrappedColumn = provider.WrapColumn("test");
            var wrappedString = provider.WrapString("test");

            Assert.IsTrue(wrappedColumn.Contains("test"));
            Assert.IsTrue(wrappedString.Contains("test"));
            Assert.IsTrue(wrappedColumn.Length > "test".Length);
            Assert.IsTrue(wrappedString.Length > "test".Length);
        }
    }

    [TestMethod]
    public void AllDialectProviders_HandleEmptyString()
    {
        // Arrange
        var providers = new IDatabaseDialectProvider[]
        {
            new MySqlDialectProvider(),
            new SqlServerDialectProvider(),
            new PostgreSqlDialectProvider(),
            new SQLiteDialectProvider()
        };

        // Act & Assert
        foreach (var provider in providers)
        {
            var wrappedColumn = provider.WrapColumn("");
            var wrappedString = provider.WrapString("");

            Assert.IsNotNull(wrappedColumn);
            Assert.IsNotNull(wrappedString);
            Assert.IsTrue(wrappedColumn.Length >= 2); // Should have at least opening and closing characters
            Assert.IsTrue(wrappedString.Length >= 2); // Should have at least opening and closing characters
        }
    }

    [TestMethod]
    public void AllDialectProviders_HandleSpecialCharacters()
    {
        // Arrange
        var providers = new IDatabaseDialectProvider[]
        {
            new MySqlDialectProvider(),
            new SqlServerDialectProvider(),
            new PostgreSqlDialectProvider(),
            new SQLiteDialectProvider()
        };

        var testString = "test'with\"quotes";

        // Act & Assert
        foreach (var provider in providers)
        {
            var wrappedColumn = provider.WrapColumn(testString);
            var wrappedString = provider.WrapString(testString);

            Assert.IsNotNull(wrappedColumn);
            Assert.IsNotNull(wrappedString);
            // Should handle special characters gracefully
            Assert.IsTrue(wrappedColumn.Length > testString.Length);
        }
    }
}
