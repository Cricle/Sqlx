// <copyright file="DialectWhereClauseTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests;

/// <summary>
/// Tests for dialect-specific WHERE clause generation.
/// Ensures that ExpressionExtensions.ToWhereClause correctly handles
/// different database dialects (MySQL, PostgreSQL, SQL Server, Oracle, SQLite, DB2).
/// </summary>
[TestClass]
public class DialectWhereClauseTests
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int UnitsInStock { get; set; }
        public bool Discontinued { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    #region Column Quoting Tests

    [TestMethod]
    public void MySql_ColumnQuoting_UsesBackticks()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id == 1;
        var sql = predicate.ToWhereClause(SqlDefine.MySql);
        Assert.IsTrue(sql.StartsWith("`"));
        Assert.IsTrue(sql.Contains("`id`"));
    }

    [TestMethod]
    public void PostgreSql_ColumnQuoting_UsesDoubleQuotes()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id == 1;
        var sql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        Assert.IsTrue(sql.StartsWith("\""));
        Assert.IsTrue(sql.Contains("\"id\""));
    }

    [TestMethod]
    public void SqlServer_ColumnQuoting_UsesBrackets()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id == 1;
        var sql = predicate.ToWhereClause(SqlDefine.SqlServer);
        Assert.IsTrue(sql.StartsWith("["));
        Assert.IsTrue(sql.Contains("[id]"));
    }

    [TestMethod]
    public void SQLite_ColumnQuoting_UsesBrackets()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id == 1;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.IsTrue(sql.StartsWith("["));
        Assert.IsTrue(sql.Contains("[id]"));
    }

    [TestMethod]
    public void Oracle_ColumnQuoting_UsesDoubleQuotes()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id == 1;
        var sql = predicate.ToWhereClause(SqlDefine.Oracle);
        Assert.IsTrue(sql.StartsWith("\""));
        Assert.IsTrue(sql.Contains("\"id\""));
    }

    [TestMethod]
    public void DB2_ColumnQuoting_UsesDoubleQuotes()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id == 1;
        var sql = predicate.ToWhereClause(SqlDefine.DB2);
        Assert.IsTrue(sql.StartsWith("\""));
        Assert.IsTrue(sql.Contains("\"id\""));
    }

    #endregion

    #region Boolean Literal Tests

    [TestMethod]
    public void MySql_BooleanTrue_Uses1()
    {
        Expression<Func<Product, bool>> predicate = p => p.Discontinued == true;
        var sql = predicate.ToWhereClause(SqlDefine.MySql);
        Assert.IsTrue(sql.Contains("= 1"));
    }

    [TestMethod]
    public void MySql_BooleanFalse_Uses0()
    {
        Expression<Func<Product, bool>> predicate = p => p.Discontinued == false;
        var sql = predicate.ToWhereClause(SqlDefine.MySql);
        Assert.IsTrue(sql.Contains("= 0"));
    }

    [TestMethod]
    public void PostgreSql_BooleanTrue_UsesTrue()
    {
        Expression<Func<Product, bool>> predicate = p => p.Discontinued == true;
        var sql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        Assert.IsTrue(sql.Contains("= true"));
    }

    [TestMethod]
    public void PostgreSql_BooleanFalse_UsesFalse()
    {
        Expression<Func<Product, bool>> predicate = p => p.Discontinued == false;
        var sql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        Assert.IsTrue(sql.Contains("= false"));
    }

    [TestMethod]
    public void SqlServer_BooleanTrue_Uses1()
    {
        Expression<Func<Product, bool>> predicate = p => p.Discontinued == true;
        var sql = predicate.ToWhereClause(SqlDefine.SqlServer);
        Assert.IsTrue(sql.Contains("= 1"));
    }

    [TestMethod]
    public void SQLite_BooleanTrue_Uses1()
    {
        Expression<Func<Product, bool>> predicate = p => p.Discontinued == true;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.IsTrue(sql.Contains("= 1"));
    }

    #endregion

    #region Not Equal Operator Tests

    [TestMethod]
    public void MySql_NotEqual_UsesAngleBrackets()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id != 1;
        var sql = predicate.ToWhereClause(SqlDefine.MySql);
        Assert.IsTrue(sql.Contains("<>"));
    }

    [TestMethod]
    public void PostgreSql_NotEqual_UsesAngleBrackets()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id != 1;
        var sql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        Assert.IsTrue(sql.Contains("<>"));
    }

    [TestMethod]
    public void SqlServer_NotEqual_UsesAngleBrackets()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id != 1;
        var sql = predicate.ToWhereClause(SqlDefine.SqlServer);
        Assert.IsTrue(sql.Contains("<>"));
    }

    [TestMethod]
    public void Oracle_NotEqual_UsesBangEquals()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id != 1;
        var sql = predicate.ToWhereClause(SqlDefine.Oracle);
        Assert.IsTrue(sql.Contains("!="));
    }

    [TestMethod]
    public void SQLite_NotEqual_UsesAngleBrackets()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id != 1;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.IsTrue(sql.Contains("<>"));
    }

    #endregion

    #region Snake Case Column Name Tests

    [TestMethod]
    public void AllDialects_PascalCaseProperty_ConvertedToSnakeCase()
    {
        Expression<Func<Product, bool>> predicate = p => p.ProductName == "test";
        
        var mysqlSql = predicate.ToWhereClause(SqlDefine.MySql);
        var pgsqlSql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        var sqlserverSql = predicate.ToWhereClause(SqlDefine.SqlServer);
        var sqliteSql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsTrue(mysqlSql.Contains("product_name"));
        Assert.IsTrue(pgsqlSql.Contains("product_name"));
        Assert.IsTrue(sqlserverSql.Contains("product_name"));
        Assert.IsTrue(sqliteSql.Contains("product_name"));
    }

    [TestMethod]
    public void AllDialects_MultiWordProperty_ConvertedToSnakeCase()
    {
        Expression<Func<Product, bool>> predicate = p => p.UnitsInStock > 0;
        
        var mysqlSql = predicate.ToWhereClause(SqlDefine.MySql);
        var pgsqlSql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        var sqlserverSql = predicate.ToWhereClause(SqlDefine.SqlServer);
        var sqliteSql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsTrue(mysqlSql.Contains("units_in_stock"));
        Assert.IsTrue(pgsqlSql.Contains("units_in_stock"));
        Assert.IsTrue(sqlserverSql.Contains("units_in_stock"));
        Assert.IsTrue(sqliteSql.Contains("units_in_stock"));
    }

    #endregion

    #region Complex Expression Tests

    [TestMethod]
    public void AllDialects_AndExpression_GeneratesCorrectSql()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id > 0 && p.Discontinued == false;
        
        var mysqlSql = predicate.ToWhereClause(SqlDefine.MySql);
        var pgsqlSql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        var sqlserverSql = predicate.ToWhereClause(SqlDefine.SqlServer);
        var sqliteSql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsTrue(mysqlSql.Contains("AND"));
        Assert.IsTrue(pgsqlSql.Contains("AND"));
        Assert.IsTrue(sqlserverSql.Contains("AND"));
        Assert.IsTrue(sqliteSql.Contains("AND"));
    }

    [TestMethod]
    public void AllDialects_OrExpression_GeneratesCorrectSql()
    {
        Expression<Func<Product, bool>> predicate = p => p.Id == 1 || p.Id == 2;
        
        var mysqlSql = predicate.ToWhereClause(SqlDefine.MySql);
        var pgsqlSql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        var sqlserverSql = predicate.ToWhereClause(SqlDefine.SqlServer);
        var sqliteSql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsTrue(mysqlSql.Contains("OR"));
        Assert.IsTrue(pgsqlSql.Contains("OR"));
        Assert.IsTrue(sqlserverSql.Contains("OR"));
        Assert.IsTrue(sqliteSql.Contains("OR"));
    }

    [TestMethod]
    public void AllDialects_DecimalComparison_GeneratesCorrectSql()
    {
        Expression<Func<Product, bool>> predicate = p => p.UnitPrice >= 10.50m;
        
        var mysqlSql = predicate.ToWhereClause(SqlDefine.MySql);
        var pgsqlSql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        var sqlserverSql = predicate.ToWhereClause(SqlDefine.SqlServer);
        var sqliteSql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsTrue(mysqlSql.Contains("unit_price"));
        Assert.IsTrue(pgsqlSql.Contains("unit_price"));
        Assert.IsTrue(sqlserverSql.Contains("unit_price"));
        Assert.IsTrue(sqliteSql.Contains("unit_price"));
        
        Assert.IsTrue(mysqlSql.Contains(">="));
        Assert.IsTrue(pgsqlSql.Contains(">="));
        Assert.IsTrue(sqlserverSql.Contains(">="));
        Assert.IsTrue(sqliteSql.Contains(">="));
    }

    #endregion

    #region String Literal Tests

    [TestMethod]
    public void AllDialects_StringLiteral_UsesSingleQuotes()
    {
        Expression<Func<Product, bool>> predicate = p => p.ProductName == "Test Product";
        
        var mysqlSql = predicate.ToWhereClause(SqlDefine.MySql);
        var pgsqlSql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        var sqlserverSql = predicate.ToWhereClause(SqlDefine.SqlServer);
        var sqliteSql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsTrue(mysqlSql.Contains("'Test Product'"));
        Assert.IsTrue(pgsqlSql.Contains("'Test Product'"));
        Assert.IsTrue(sqlserverSql.Contains("'Test Product'"));
        Assert.IsTrue(sqliteSql.Contains("'Test Product'"));
    }

    #endregion

    #region Constant Value Tests

    [TestMethod]
    public void AllDialects_ConstantValue_GeneratesCorrectSql()
    {
        Expression<Func<Product, bool>> predicate = p => p.UnitPrice > 100;
        
        var mysqlSql = predicate.ToWhereClause(SqlDefine.MySql);
        var pgsqlSql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        var sqlserverSql = predicate.ToWhereClause(SqlDefine.SqlServer);
        var sqliteSql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsTrue(mysqlSql.Contains("100"));
        Assert.IsTrue(pgsqlSql.Contains("100"));
        Assert.IsTrue(sqlserverSql.Contains("100"));
        Assert.IsTrue(sqliteSql.Contains("100"));
    }

    #endregion
}
