// <copyright file="AggregateParserTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Sqlx.Tests.Expressions.FunctionParsers;

/// <summary>
/// Tests for AggregateParser covering all aggregate function branches.
/// </summary>
[TestClass]
public class AggregateParserTests
{
    [TestMethod]
    public void GetStringAgg_SqlServer_GeneratesStringAggFunction()
    {
        // Arrange
        var dialect = new SqlServerDialect();
        var col = "[name]";
        var sep = "','";

        // Act
        var result = InvokeGetStringAgg(dialect, col, sep);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("STRING_AGG([name], ',')", result);
    }

    [TestMethod]
    public void GetStringAgg_MySQL_GeneratesGroupConcatWithSeparator()
    {
        // Arrange
        var dialect = new MySqlDialect();
        var col = "`name`";
        var sep = "','";

        // Act
        var result = InvokeGetStringAgg(dialect, col, sep);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("GROUP_CONCAT(`name` SEPARATOR ',')", result);
    }

    [TestMethod]
    public void GetStringAgg_SQLite_GeneratesGroupConcatWithComma()
    {
        // Arrange
        var dialect = new SQLiteDialect();
        var col = "[name]";
        var sep = "','";

        // Act
        var result = InvokeGetStringAgg(dialect, col, sep);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("GROUP_CONCAT([name], ',')", result);
    }

    [TestMethod]
    public void GetStringAgg_Oracle_GeneratesListAgg()
    {
        // Arrange
        var dialect = new OracleDialect();
        var col = "\"name\"";
        var sep = "','";

        // Act
        var result = InvokeGetStringAgg(dialect, col, sep);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("LISTAGG(\"name\", ',') WITHIN GROUP (ORDER BY \"name\")", result);
    }

    [TestMethod]
    public void GetStringAgg_PostgreSQL_GeneratesStringAggFunction()
    {
        // Arrange
        var dialect = new PostgreSqlDialect();
        var col = "\"name\"";
        var sep = "','";

        // Act
        var result = InvokeGetStringAgg(dialect, col, sep);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("STRING_AGG(\"name\", ',')", result);
    }

    [TestMethod]
    public void GetStringAgg_DB2_GeneratesStringAggFunction()
    {
        // Arrange
        var dialect = new DB2Dialect();
        var col = "\"name\"";
        var sep = "','";

        // Act
        var result = InvokeGetStringAgg(dialect, col, sep);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("STRING_AGG(\"name\", ',')", result);
    }

    private static string? InvokeGetStringAgg(SqlDialect dialect, string col, string sep)
    {
        // Use reflection to call the private GetStringAgg method
        var aggregateParserType = typeof(SqlDialect).Assembly.GetType("Sqlx.Expressions.AggregateParser");
        Assert.IsNotNull(aggregateParserType, "AggregateParser type not found");

        var method = aggregateParserType.GetMethod("GetStringAgg",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method, "GetStringAgg method not found");

        return method.Invoke(null, new object[] { dialect, col, sep }) as string;
    }
}
