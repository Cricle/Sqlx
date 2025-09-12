// -----------------------------------------------------------------------
// <copyright file="DatabaseDialectFactoryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using Sqlx.SqlGen;
using System;

namespace Sqlx.Tests.Core;

[TestClass]
public class DatabaseDialectFactoryTests
{
    [TestMethod]
    public void GetDialectProvider_WithMySqlType_ReturnsMySqlProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.MySql);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(MySqlDialectProvider));
        Assert.AreEqual(SqlDefineTypes.MySql, provider.DialectType);
    }

    [TestMethod]
    public void GetDialectProvider_WithSqlServerType_ReturnsSqlServerProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.SqlServer);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
        Assert.AreEqual(SqlDefineTypes.SqlServer, provider.DialectType);
    }

    [TestMethod]
    public void GetDialectProvider_WithPostgresqlType_ReturnsPostgreSqlProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.Postgresql);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(PostgreSqlDialectProvider));
        Assert.AreEqual(SqlDefineTypes.Postgresql, provider.DialectType);
    }

    [TestMethod]
    public void GetDialectProvider_WithSQLiteType_ReturnsSQLiteProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.SQLite);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(SQLiteDialectProvider));
        Assert.AreEqual(SqlDefineTypes.SQLite, provider.DialectType);
    }

    [TestMethod]
    public void GetDialectProvider_WithOracleType_ThrowsUnsupportedDialectException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<UnsupportedDialectException>(() => 
            DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.Oracle));
        
        Assert.AreEqual("Oracle (support removed to reduce complexity - use PostgreSQL instead)", exception.DialectName);
        Assert.AreEqual("SQLX003", exception.ErrorCode);
    }

    [TestMethod]
    public void GetDialectProvider_WithDB2Type_ThrowsUnsupportedDialectException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<UnsupportedDialectException>(() => 
            DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.DB2));
        
        Assert.AreEqual("DB2 (support removed to reduce complexity - use PostgreSQL instead)", exception.DialectName);
        Assert.AreEqual("SQLX003", exception.ErrorCode);
    }

    [TestMethod]
    public void GetDialectProvider_WithInvalidType_ThrowsUnsupportedDialectException()
    {
        // Arrange
        var invalidType = (SqlDefineTypes)999;

        // Act & Assert
        var exception = Assert.ThrowsException<UnsupportedDialectException>(() => 
            DatabaseDialectFactory.GetDialectProvider(invalidType));
        
        Assert.AreEqual("999", exception.DialectName);
    }

    [TestMethod]
    public void GetDialectProvider_WithMySqlDefine_ReturnsMySqlProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefine.MySql);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(MySqlDialectProvider));
        Assert.AreEqual(SqlDefineTypes.MySql, provider.DialectType);
    }

    [TestMethod]
    public void GetDialectProvider_WithSqlServerDefine_ReturnsSqlServerProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefine.SqlServer);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
        Assert.AreEqual(SqlDefineTypes.SqlServer, provider.DialectType);
    }

    [TestMethod]
    public void GetDialectProvider_WithPgSqlDefine_ReturnsPostgreSqlProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefine.PgSql);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(PostgreSqlDialectProvider));
        Assert.AreEqual(SqlDefineTypes.Postgresql, provider.DialectType);
    }

    [TestMethod]
    public void GetDialectProvider_WithSQLiteDefine_ReturnsSQLiteProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(SqlDefine.SQLite);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(SQLiteDialectProvider));
        Assert.AreEqual(SqlDefineTypes.SQLite, provider.DialectType);
    }

    [TestMethod]
    public void GetDialectProvider_WithOracleDefine_ThrowsUnsupportedDialectException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<UnsupportedDialectException>(() => 
            DatabaseDialectFactory.GetDialectProvider(SqlDefine.Oracle));
        
        Assert.IsTrue(exception.Message.Contains("Oracle"));
    }

    [TestMethod]
    public void GetDialectProvider_WithDB2Define_ThrowsUnsupportedDialectException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<UnsupportedDialectException>(() => 
            DatabaseDialectFactory.GetDialectProvider(SqlDefine.DB2));
        
        Assert.IsTrue(exception.Message.Contains("DB2"));
    }

    [TestMethod]
    public void GetDialectProvider_WithCustomMySqlLikeDefine_ReturnsMySqlProvider()
    {
        // Arrange
        var customDefine = new SqlDefine("`", "`", "'", "'", "@");

        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(customDefine);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(MySqlDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_WithCustomPostgresqlLikeDefine_ReturnsPostgreSqlProvider()
    {
        // Arrange
        var customDefine = new SqlDefine("\"", "\"", "'", "'", "$");

        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(customDefine);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(PostgreSqlDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_WithCustomOracleLikeDefine_ThrowsUnsupportedDialectException()
    {
        // Arrange
        var customDefine = new SqlDefine("\"", "\"", "'", "'", ":");

        // Act & Assert
        var exception = Assert.ThrowsException<UnsupportedDialectException>(() => 
            DatabaseDialectFactory.GetDialectProvider(customDefine));
        
        Assert.IsTrue(exception.Message.Contains("Oracle"));
    }

    [TestMethod]
    public void GetDialectProvider_WithCustomDB2LikeDefine_ThrowsUnsupportedDialectException()
    {
        // Arrange
        var customDefine = new SqlDefine("\"", "\"", "'", "'", "?");

        // Act & Assert
        var exception = Assert.ThrowsException<UnsupportedDialectException>(() => 
            DatabaseDialectFactory.GetDialectProvider(customDefine));
        
        Assert.IsTrue(exception.Message.Contains("DB2"));
    }

    [TestMethod]
    public void GetDialectProvider_WithCustomSQLiteLikeDefine_ReturnsSQLiteProvider()
    {
        // Arrange
        var customDefine = new SqlDefine("[", "]", "'", "'", "@sqlite");

        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(customDefine);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(SQLiteDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_WithCustomSqlServerLikeDefine_ReturnsSqlServerProvider()
    {
        // Arrange
        var customDefine = new SqlDefine("[", "]", "'", "'", "@");

        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(customDefine);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_WithUnknownCustomDefine_ReturnsSqlServerProviderAsDefault()
    {
        // Arrange
        var customDefine = new SqlDefine("<<", ">>", "'", "'", "#");

        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(customDefine);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
    }

    [TestMethod]
    public void GetDialectProvider_CallMultipleTimes_ReturnsNewInstancesEachTime()
    {
        // Act
        var provider1 = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.MySql);
        var provider2 = DatabaseDialectFactory.GetDialectProvider(SqlDefineTypes.MySql);

        // Assert
        Assert.AreNotSame(provider1, provider2);
        Assert.AreEqual(provider1.GetType(), provider2.GetType());
    }

    [TestMethod]
    public void GetDialectProvider_WithAllSupportedTypes_CreatesCorrectProviders()
    {
        // Arrange
        var supportedTypes = new[]
        {
            SqlDefineTypes.MySql,
            SqlDefineTypes.SqlServer,
            SqlDefineTypes.Postgresql,
            SqlDefineTypes.SQLite
        };

        // Act & Assert
        foreach (var type in supportedTypes)
        {
            var provider = DatabaseDialectFactory.GetDialectProvider(type);
            Assert.IsNotNull(provider);
            Assert.AreEqual(type, provider.DialectType);
        }
    }
}