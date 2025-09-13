// -----------------------------------------------------------------------
// <copyright file="DatabaseDialectSimpleTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.SqlGen;

/// <summary>
/// Simple tests for database dialect functionality.
/// Tests basic dialect detection and enum validation.
/// </summary>
[TestClass]
public class DatabaseDialectSimpleTests
{
    /// <summary>
    /// Tests that SqlDefineTypes enum has expected values.
    /// </summary>
    [TestMethod]
    public void SqlDefineTypes_EnumValues_AreCorrectlyDefined()
    {
        // Test that all expected dialect types exist
        Assert.IsTrue(Enum.IsDefined(typeof(SqlDefineTypes), SqlDefineTypes.MySql), 
            "MySql should be defined");
        Assert.IsTrue(Enum.IsDefined(typeof(SqlDefineTypes), SqlDefineTypes.SqlServer), 
            "SqlServer should be defined");
        Assert.IsTrue(Enum.IsDefined(typeof(SqlDefineTypes), SqlDefineTypes.Postgresql), 
            "Postgresql should be defined");
        Assert.IsTrue(Enum.IsDefined(typeof(SqlDefineTypes), SqlDefineTypes.SQLite), 
            "SQLite should be defined");
        Assert.IsTrue(Enum.IsDefined(typeof(SqlDefineTypes), SqlDefineTypes.Oracle), 
            "Oracle should be defined");
        Assert.IsTrue(Enum.IsDefined(typeof(SqlDefineTypes), SqlDefineTypes.DB2), 
            "DB2 should be defined");
    }

    /// <summary>
    /// Tests that SqlDefineTypes enum values are in expected numeric order.
    /// </summary>
    [TestMethod]
    public void SqlDefineTypes_EnumValues_AreInCorrectOrder()
    {
        Assert.AreEqual(0, (int)SqlDefineTypes.MySql, "MySql should be 0");
        Assert.AreEqual(1, (int)SqlDefineTypes.SqlServer, "SqlServer should be 1");
        Assert.AreEqual(2, (int)SqlDefineTypes.Postgresql, "Postgresql should be 2");
        Assert.AreEqual(3, (int)SqlDefineTypes.Oracle, "Oracle should be 3");
        Assert.AreEqual(4, (int)SqlDefineTypes.DB2, "DB2 should be 4");
        Assert.AreEqual(5, (int)SqlDefineTypes.SQLite, "SQLite should be 5");
    }

    /// <summary>
    /// Tests that all enum values are unique.
    /// </summary>
    [TestMethod]
    public void SqlDefineTypes_EnumValues_AreUnique()
    {
        var allValues = Enum.GetValues<SqlDefineTypes>();
        var uniqueValues = allValues.Distinct().ToArray();
        
        Assert.AreEqual(allValues.Length, uniqueValues.Length, 
            "All SqlDefineTypes values should be unique");
    }

    /// <summary>
    /// Tests enum string representations.
    /// </summary>
    [TestMethod]
    public void SqlDefineTypes_StringRepresentations_AreCorrect()
    {
        Assert.AreEqual("MySql", SqlDefineTypes.MySql.ToString(), 
            "MySql should have correct string representation");
        Assert.AreEqual("SqlServer", SqlDefineTypes.SqlServer.ToString(), 
            "SqlServer should have correct string representation");
        Assert.AreEqual("Postgresql", SqlDefineTypes.Postgresql.ToString(), 
            "Postgresql should have correct string representation");
        Assert.AreEqual("Oracle", SqlDefineTypes.Oracle.ToString(), 
            "Oracle should have correct string representation");
        Assert.AreEqual("DB2", SqlDefineTypes.DB2.ToString(), 
            "DB2 should have correct string representation");
        Assert.AreEqual("SQLite", SqlDefineTypes.SQLite.ToString(), 
            "SQLite should have correct string representation");
    }

    /// <summary>
    /// Tests enum parsing from strings.
    /// </summary>
    [TestMethod]
    public void SqlDefineTypes_Parsing_WorksCorrectly()
    {
        // Test parsing exact names
        Assert.AreEqual(SqlDefineTypes.MySql, 
            Enum.Parse<SqlDefineTypes>("MySql"), 
            "Should parse 'MySql' correctly");
        Assert.AreEqual(SqlDefineTypes.SqlServer, 
            Enum.Parse<SqlDefineTypes>("SqlServer"), 
            "Should parse 'SqlServer' correctly");
        Assert.AreEqual(SqlDefineTypes.Postgresql, 
            Enum.Parse<SqlDefineTypes>("Postgresql"), 
            "Should parse 'Postgresql' correctly");
        Assert.AreEqual(SqlDefineTypes.SQLite, 
            Enum.Parse<SqlDefineTypes>("SQLite"), 
            "Should parse 'SQLite' correctly");

        // Test case-insensitive parsing
        Assert.AreEqual(SqlDefineTypes.MySql, 
            Enum.Parse<SqlDefineTypes>("mysql", true), 
            "Should parse 'mysql' case-insensitively");
        Assert.AreEqual(SqlDefineTypes.SqlServer, 
            Enum.Parse<SqlDefineTypes>("sqlserver", true), 
            "Should parse 'sqlserver' case-insensitively");
    }

    /// <summary>
    /// Tests that TryParse works for valid values.
    /// </summary>
    [TestMethod]
    public void SqlDefineTypes_TryParse_HandlesValidAndInvalidValues()
    {
        // Test valid values
        Assert.IsTrue(Enum.TryParse<SqlDefineTypes>("MySql", out var mysqlResult), 
            "Should successfully parse 'MySql'");
        Assert.AreEqual(SqlDefineTypes.MySql, mysqlResult, 
            "Parsed MySql should equal enum value");

        Assert.IsTrue(Enum.TryParse<SqlDefineTypes>("SqlServer", out var sqlServerResult), 
            "Should successfully parse 'SqlServer'");
        Assert.AreEqual(SqlDefineTypes.SqlServer, sqlServerResult, 
            "Parsed SqlServer should equal enum value");

        // Test invalid values
        Assert.IsFalse(Enum.TryParse<SqlDefineTypes>("InvalidDialect", out var invalidResult), 
            "Should fail to parse invalid dialect name");
        Assert.AreEqual(default(SqlDefineTypes), invalidResult, 
            "Invalid parse should return default value");

        Assert.IsFalse(Enum.TryParse<SqlDefineTypes>("", out var emptyResult), 
            "Should fail to parse empty string");
        Assert.AreEqual(default(SqlDefineTypes), emptyResult, 
            "Empty parse should return default value");
    }

    /// <summary>
    /// Tests that commonly supported dialects are available.
    /// </summary>
    [TestMethod]
    public void SqlDefineTypes_CommonDialects_AreSupported()
    {
        var commonDialects = new[]
        {
            SqlDefineTypes.MySql,
            SqlDefineTypes.SqlServer,
            SqlDefineTypes.Postgresql,
            SqlDefineTypes.SQLite
        };

        foreach (var dialect in commonDialects)
        {
            Assert.IsTrue(Enum.IsDefined(typeof(SqlDefineTypes), dialect), 
                $"Common dialect {dialect} should be supported");
        }
    }

    /// <summary>
    /// Tests dialect availability for enterprise scenarios.
    /// </summary>
    [TestMethod]
    public void SqlDefineTypes_EnterpriseDialects_AreAvailable()
    {
        var enterpriseDialects = new[]
        {
            SqlDefineTypes.Oracle,
            SqlDefineTypes.DB2
        };

        foreach (var dialect in enterpriseDialects)
        {
            Assert.IsTrue(Enum.IsDefined(typeof(SqlDefineTypes), dialect), 
                $"Enterprise dialect {dialect} should be available");
        }
    }

    /// <summary>
    /// Tests that default dialect behavior is predictable.
    /// </summary>
    [TestMethod]
    public void SqlDefineTypes_DefaultValue_IsConsistent()
    {
        var defaultDialect = default(SqlDefineTypes);
        Assert.AreEqual(SqlDefineTypes.MySql, defaultDialect, 
            "Default SqlDefineTypes should be MySql (value 0)");

        // Test that the first enum value is MySql
        var firstValue = Enum.GetValues<SqlDefineTypes>().First();
        Assert.AreEqual(SqlDefineTypes.MySql, firstValue, 
            "First enum value should be MySql");
    }

    /// <summary>
    /// Tests enum behavior with numeric values.
    /// </summary>
    [TestMethod]
    public void SqlDefineTypes_NumericValues_ConvertCorrectly()
    {
        // Test conversion from numeric values
        Assert.AreEqual(SqlDefineTypes.MySql, (SqlDefineTypes)0, 
            "Numeric 0 should convert to MySql");
        Assert.AreEqual(SqlDefineTypes.SqlServer, (SqlDefineTypes)1, 
            "Numeric 1 should convert to SqlServer");
        Assert.AreEqual(SqlDefineTypes.Postgresql, (SqlDefineTypes)2, 
            "Numeric 2 should convert to Postgresql");
        Assert.AreEqual(SqlDefineTypes.Oracle, (SqlDefineTypes)3, 
            "Numeric 3 should convert to Oracle");
        Assert.AreEqual(SqlDefineTypes.DB2, (SqlDefineTypes)4, 
            "Numeric 4 should convert to DB2");
        Assert.AreEqual(SqlDefineTypes.SQLite, (SqlDefineTypes)5, 
            "Numeric 5 should convert to SQLite");

        // Test conversion to numeric values
        Assert.AreEqual(0, (int)SqlDefineTypes.MySql, "MySql should convert to 0");
        Assert.AreEqual(1, (int)SqlDefineTypes.SqlServer, "SqlServer should convert to 1");
        Assert.AreEqual(2, (int)SqlDefineTypes.Postgresql, "Postgresql should convert to 2");
        Assert.AreEqual(3, (int)SqlDefineTypes.Oracle, "Oracle should convert to 3");
        Assert.AreEqual(4, (int)SqlDefineTypes.DB2, "DB2 should convert to 4");
        Assert.AreEqual(5, (int)SqlDefineTypes.SQLite, "SQLite should convert to 5");
    }

    /// <summary>
    /// Tests that enum covers major database vendors.
    /// </summary>
    [TestMethod]
    public void SqlDefineTypes_MajorVendors_AreRepresented()
    {
        var allDialects = Enum.GetValues<SqlDefineTypes>();
        var dialectNames = allDialects.Select(d => d.ToString().ToLowerInvariant()).ToArray();

        // Check that major database vendors are represented
        Assert.IsTrue(dialectNames.Any(name => name.Contains("mysql")), 
            "MySQL should be represented");
        Assert.IsTrue(dialectNames.Any(name => name.Contains("sql")), 
            "SQL Server should be represented");
        Assert.IsTrue(dialectNames.Any(name => name.Contains("postgres")), 
            "PostgreSQL should be represented");
        Assert.IsTrue(dialectNames.Any(name => name.Contains("sqlite")), 
            "SQLite should be represented");
        Assert.IsTrue(dialectNames.Any(name => name.Contains("oracle")), 
            "Oracle should be represented");
    }
}
