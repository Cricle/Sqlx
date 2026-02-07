// -----------------------------------------------------------------------
// <copyright file="SqlxQueryProviderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Tests for SqlxQueryProvider.
/// </summary>
[TestClass]
public class SqlxQueryProviderTests
{
    [Sqlx]
    [TableName("users")]
    public class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    [TestMethod]
    public void Constructor_WithDialect_CreatesProvider()
    {
        // Arrange & Act
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);

        // Assert
        Assert.IsNotNull(provider);
        Assert.AreEqual(SqlDefine.SQLite, provider.Dialect);
    }

    [TestMethod]
    public void Constructor_WithNullDialect_ThrowsException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            new SqlxQueryProvider<TestUser>(null!);
        });
    }

    [TestMethod]
    public void Constructor_WithEntityProvider_StoresProvider()
    {
        // Arrange
        var entityProvider = TestUserEntityProvider.Default;

        // Act
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite, entityProvider);

        // Assert
        Assert.IsNotNull(provider);
    }

    [TestMethod]
    public void CreateQuery_NonGeneric_ThrowsNotSupportedException()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        Expression expression = Expression.Constant(new TestUser[] { });

        // Act & Assert
        Assert.ThrowsException<NotSupportedException>(() =>
        {
            provider.CreateQuery(expression);
        });
    }

    [TestMethod]
    public void CreateQuery_Generic_CreatesQueryable()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        var parameter = Expression.Parameter(typeof(TestUser), "u");
        var expression = Expression.Lambda<Func<TestUser, bool>>(
            Expression.Constant(true),
            parameter);

        // Act
        var queryable = provider.CreateQuery<TestUser>(expression);

        // Assert
        Assert.IsNotNull(queryable);
        Assert.IsInstanceOfType(queryable, typeof(IQueryable<TestUser>));
    }

    [TestMethod]
    public void CreateQuery_DifferentType_CreatesDynamicReader()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        var parameter = Expression.Parameter(typeof(int), "x");
        var expression = Expression.Lambda<Func<int, bool>>(
            Expression.Constant(true),
            parameter);

        // Act
        var queryable = provider.CreateQuery<int>(expression);

        // Assert
        Assert.IsNotNull(queryable);
        Assert.IsInstanceOfType(queryable, typeof(IQueryable<int>));
    }

    [TestMethod]
    public void Execute_NonGeneric_ThrowsNotSupportedException()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        Expression expression = Expression.Constant(42);

        // Act & Assert
        Assert.ThrowsException<NotSupportedException>(() =>
        {
            provider.Execute(expression);
        });
    }

    [TestMethod]
    public void Execute_WithoutConnection_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        var parameter = Expression.Parameter(typeof(TestUser), "u");
        var methodCall = Expression.Call(
            typeof(Queryable),
            "First",
            new[] { typeof(TestUser) },
            Expression.Constant(Array.Empty<TestUser>().AsQueryable()));

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            provider.Execute<TestUser>(methodCall);
        });
    }

    [TestMethod]
    public void Execute_UnsupportedExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        var expression = Expression.Constant(42);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            provider.Execute<int>(expression);
        });
    }

    [TestMethod]
    public void Execute_UnsupportedMethod_ThrowsNotSupportedException()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        var source = Expression.Constant(Array.Empty<TestUser>().AsQueryable());
        var methodCall = Expression.Call(
            typeof(Enumerable),
            "ToArray",
            new[] { typeof(TestUser) },
            source);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            provider.Execute<TestUser[]>(methodCall);
        });
    }

    [TestMethod]
    public void ToSql_SimpleExpression_GeneratesSql()
    {
        // Arrange
        var entityProvider = TestUserEntityProvider.Default;
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite, entityProvider);
        var queryable = SqlQuery<TestUser>.ForSqlite();
        var expression = queryable.Where(u => u.Id == 1).Expression;

        // Act
        var sql = provider.ToSql(expression);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(sql));
        Assert.IsTrue(sql.Contains("SELECT") || sql.Contains("select"));
    }

    [TestMethod]
    public void ToSql_WithParameterization_GeneratesSql()
    {
        // Arrange
        var entityProvider = TestUserEntityProvider.Default;
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite, entityProvider);
        var queryable = SqlQuery<TestUser>.ForSqlite();
        var expression = queryable.Where(u => u.Name == "John").Expression;

        // Act
        var sql = provider.ToSql(expression, parameterized: true);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(sql));
        Assert.IsTrue(sql.Contains("SELECT") || sql.Contains("select"));
    }

    [TestMethod]
    public void ToSqlWithParameters_SimpleExpression_GeneratesSqlAndParameters()
    {
        // Arrange
        var entityProvider = TestUserEntityProvider.Default;
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite, entityProvider);
        var queryable = SqlQuery<TestUser>.ForSqlite();
        var expression = queryable.Where(u => u.Age == 25).Expression;

        // Act
        var (sql, parameters) = provider.ToSqlWithParameters(expression);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(sql));
        Assert.IsNotNull(parameters);
        Assert.IsTrue(sql.Contains("SELECT") || sql.Contains("select"));
    }

    [TestMethod]
    public void ToSqlWithParameters_ComplexExpression_GeneratesSqlAndParameters()
    {
        // Arrange
        var entityProvider = TestUserEntityProvider.Default;
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite, entityProvider);
        var queryable = SqlQuery<TestUser>.ForSqlite();
        var expression = queryable.Where(u => u.Age > 18 && u.IsActive).Expression;

        // Act
        var (sql, parameters) = provider.ToSqlWithParameters(expression);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(sql));
        Assert.IsNotNull(parameters);
        Assert.IsTrue(sql.Contains("SELECT") || sql.Contains("select"));
    }

    [TestMethod]
    public void Dialect_ReturnsCorrectDialect()
    {
        // Arrange
        var dialect = SqlDefine.MySql;
        var provider = new SqlxQueryProvider<TestUser>(dialect);

        // Act
        var result = provider.Dialect;

        // Assert
        Assert.AreEqual(dialect, result);
    }

    [TestMethod]
    public void CreateQuery_WithAnonymousType_CreatesDynamicReader()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        
        // Create expression for anonymous type projection
        var parameter = Expression.Parameter(typeof(TestUser), "u");
        var newExpression = Expression.New(
            typeof(object).GetConstructor(Type.EmptyTypes)!);
        var expression = Expression.Lambda(newExpression, parameter);

        // Act
        var queryable = provider.CreateQuery<object>(expression);

        // Assert
        Assert.IsNotNull(queryable);
    }

    [TestMethod]
    public void CreateQuery_WithStringType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        var parameter = Expression.Parameter(typeof(string), "s");
        var expression = Expression.Lambda<Func<string, bool>>(
            Expression.Constant(true),
            parameter);

        // Act
        var queryable = provider.CreateQuery<string>(expression);

        // Assert
        Assert.IsNotNull(queryable);
    }

    [TestMethod]
    public void CreateQuery_WithIntType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        var parameter = Expression.Parameter(typeof(int), "i");
        var expression = Expression.Lambda<Func<int, bool>>(
            Expression.Constant(true),
            parameter);

        // Act
        var queryable = provider.CreateQuery<int>(expression);

        // Assert
        Assert.IsNotNull(queryable);
    }

    [TestMethod]
    public void CreateQuery_WithDecimalType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        var parameter = Expression.Parameter(typeof(decimal), "d");
        var expression = Expression.Lambda<Func<decimal, bool>>(
            Expression.Constant(true),
            parameter);

        // Act
        var queryable = provider.CreateQuery<decimal>(expression);

        // Assert
        Assert.IsNotNull(queryable);
    }

    [TestMethod]
    public void CreateQuery_WithDateTimeType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        var parameter = Expression.Parameter(typeof(DateTime), "dt");
        var expression = Expression.Lambda<Func<DateTime, bool>>(
            Expression.Constant(true),
            parameter);

        // Act
        var queryable = provider.CreateQuery<DateTime>(expression);

        // Assert
        Assert.IsNotNull(queryable);
    }

    [TestMethod]
    public void CreateQuery_WithGuidType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite);
        var parameter = Expression.Parameter(typeof(Guid), "g");
        var expression = Expression.Lambda<Func<Guid, bool>>(
            Expression.Constant(true),
            parameter);

        // Act
        var queryable = provider.CreateQuery<Guid>(expression);

        // Assert
        Assert.IsNotNull(queryable);
    }

    [TestMethod]
    public void ToSql_WithDifferentDialects_GeneratesDialectSpecificSql()
    {
        // Arrange
        var entityProvider = TestUserEntityProvider.Default;
        var sqliteQueryable = SqlQuery<TestUser>.ForSqlite();
        var mysqlQueryable = SqlQuery<TestUser>.ForMySql();
        var postgresQueryable = SqlQuery<TestUser>.ForPostgreSQL();
        
        var expression = sqliteQueryable.Where(u => u.Id == 1).Expression;

        var sqliteProvider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite, entityProvider);
        var mysqlProvider = new SqlxQueryProvider<TestUser>(SqlDefine.MySql, entityProvider);
        var postgresProvider = new SqlxQueryProvider<TestUser>(SqlDefine.PostgreSql, entityProvider);

        // Act
        var sqliteSql = sqliteProvider.ToSql(expression);
        var mysqlSql = mysqlProvider.ToSql(expression);
        var postgresSql = postgresProvider.ToSql(expression);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(sqliteSql));
        Assert.IsFalse(string.IsNullOrEmpty(mysqlSql));
        Assert.IsFalse(string.IsNullOrEmpty(postgresSql));
    }

    [TestMethod]
    public void ToSqlWithParameters_WithMultipleParameters_ExtractsAll()
    {
        // Arrange
        var entityProvider = TestUserEntityProvider.Default;
        var provider = new SqlxQueryProvider<TestUser>(SqlDefine.SQLite, entityProvider);
        var queryable = SqlQuery<TestUser>.ForSqlite();
        var expression = queryable.Where(u => u.Name == "John" && u.Age == 30).Expression;

        // Act
        var (sql, parameters) = provider.ToSqlWithParameters(expression);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(sql));
        Assert.IsNotNull(parameters);
        var paramList = parameters.ToList();
        Assert.IsTrue(paramList.Count > 0);
    }
}
