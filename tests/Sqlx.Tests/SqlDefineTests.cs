// -----------------------------------------------------------------------
// <copyright file="SqlDefineTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for SqlDefine functionality.
/// </summary>
[TestClass]
public class SqlDefineTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests that SqlDefine constants are generated correctly through source generation.
    /// </summary>
    [TestMethod]
    public void SqlDefine_GeneratesCorrectConstants()
    {
        string sourceCode = @"
using Sqlx.Annotations;
using System.Data.Common;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var mysql = SqlDefine.MySql;
            var sqlServer = SqlDefine.SqlServer;
            var postgres = SqlDefine.PgSql;
        }
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode, "Source generator should produce code");
        Assert.IsTrue(generatedCode.Contains("SqlDefine"), "Generated code should contain SqlDefine class");

        // Verify all dialect constants are present
        Assert.IsTrue(generatedCode.Contains("MySql ="), "Generated code should contain MySql constant");
        Assert.IsTrue(generatedCode.Contains("SqlServer ="), "Generated code should contain SqlServer constant");
        Assert.IsTrue(generatedCode.Contains("PgSql ="), "Generated code should contain PgSql constant");
    }

    /// <summary>
    /// Tests MySQL dialect configuration values through actual code generation.
    /// </summary>
    [TestMethod]
    public void SqlDefine_MySql_HasCorrectValues()
    {
        string sourceCode = @"
using Sqlx.Annotations;
using System.Data.Common;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var (left, right, strLeft, strRight, prefix) = SqlDefine.MySql;
        }
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // MySQL should use backticks for columns and @ for parameters
        Assert.IsTrue(
            generatedCode.Contains("MySql = (\"`\", \"`\", \"'\", \"'\", \"@\")"),
            "MySQL dialect should have backticks for columns and @ prefix");
    }

    /// <summary>
    /// Tests SQL Server dialect configuration values through actual code generation.
    /// </summary>
    [TestMethod]
    public void SqlDefine_SqlServer_HasCorrectValues()
    {
        string sourceCode = @"
using Sqlx.Annotations;
using System.Data.Common;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var (left, right, strLeft, strRight, prefix) = SqlDefine.SqlServer;
        }
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // SQL Server should use brackets for columns and @ for parameters
        Assert.IsTrue(
            generatedCode.Contains("SqlServer = (\"[\", \"]\", \"'\", \"'\", \"@\")"),
            "SQL Server dialect should have brackets for columns and @ prefix");
    }

    /// <summary>
    /// Tests PostgreSQL dialect configuration values through actual code generation.
    /// </summary>
    [TestMethod]
    public void SqlDefine_PostgreSql_HasCorrectValues()
    {
        string sourceCode = @"
using Sqlx.Annotations;
using System.Data.Common;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var (left, right, strLeft, strRight, prefix) = SqlDefine.PgSql;
        }
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // PostgreSQL should use double quotes for columns and $ for parameters
        Assert.IsTrue(
            generatedCode.Contains("PgSql = (\"\\u0022\", \"\\u0022\", \"'\", \"'\", \"$\")"),
            "PostgreSQL dialect should have double quotes for columns and $ prefix");
    }

    /// <summary>
    /// Data-driven test for different SQL dialect configurations.
    /// </summary>
    /// <param name="dialectName">The SQL dialect name.</param>
    /// <param name="expectedLeftQuote">Expected left quote character.</param>
    /// <param name="expectedRightQuote">Expected right quote character.</param>
    /// <param name="expectedPrefix">Expected parameter prefix.</param>
    [TestMethod]
    [DataRow("MySql", "`", "`", "@")]
    [DataRow("SqlServer", "[", "]", "@")]
    [DataRow("PgSql", "\\u0022", "\\u0022", "$")]
    public void SqlDefine_DialectConstants_HaveCorrectFormat(string dialectName, string expectedLeftQuote, string expectedRightQuote, string expectedPrefix)
    {
        string sourceCode = $@"
using Sqlx.Annotations;
using System.Data.Common;

namespace TestNamespace
{{
    public class TestClass
    {{
        public void TestMethod()
        {{
            var dialect = SqlDefine.{dialectName};
        }}
    }}
}}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Verify the specific dialect constant format
        var expectedPattern = $"{dialectName} = (\"{expectedLeftQuote}\", \"{expectedRightQuote}\", \"'\", \"'\", \"{expectedPrefix}\")";
        Assert.IsTrue(generatedCode.Contains(expectedPattern), $"{dialectName} should have correct format. Expected: {expectedPattern}");
    }

    /// <summary>
    /// Tests the WrapString method for all dialects.
    /// </summary>
    [TestMethod]
    public void SqlDefine_WrapString_WorksForAllDialects()
    {
        // Test MySql
        var mysqlResult = SqlDefine.MySql.WrapString("test");
        Assert.AreEqual("'test'", mysqlResult);

        // Test SqlServer
        var sqlServerResult = SqlDefine.SqlServer.WrapString("test");
        Assert.AreEqual("'test'", sqlServerResult);

        // Test PgSql
        var pgSqlResult = SqlDefine.PgSql.WrapString("test");
        Assert.AreEqual("'test'", pgSqlResult);
    }

    /// <summary>
    /// Tests the WrapColumn method for all dialects.
    /// </summary>
    [TestMethod]
    public void SqlDefine_WrapColumn_WorksForAllDialects()
    {
        // Test MySql
        var mysqlResult = SqlDefine.MySql.WrapColumn("test");
        Assert.AreEqual("`test`", mysqlResult);

        // Test SqlServer
        var sqlServerResult = SqlDefine.SqlServer.WrapColumn("test");
        Assert.AreEqual("[test]", sqlServerResult);

        // Test PgSql
        var pgSqlResult = SqlDefine.PgSql.WrapColumn("test");
        Assert.AreEqual("\"test\"", pgSqlResult);
    }

    /// <summary>
    /// Tests the WrapString method with empty string.
    /// </summary>
    [TestMethod]
    public void SqlDefine_WrapString_WithEmptyString_ReturnsQuotedEmptyString()
    {
        var result = SqlDefine.MySql.WrapString("");
        Assert.AreEqual("''", result);
    }

    /// <summary>
    /// Tests the WrapColumn method with empty string.
    /// </summary>
    [TestMethod]
    public void SqlDefine_WrapColumn_WithEmptyString_ReturnsQuotedEmptyString()
    {
        var result = SqlDefine.MySql.WrapColumn("");
        Assert.AreEqual("``", result);
    }

    /// <summary>
    /// Tests the WrapString method with special characters.
    /// </summary>
    [TestMethod]
    public void SqlDefine_WrapString_WithSpecialCharacters_HandlesCorrectly()
    {
        var result = SqlDefine.MySql.WrapString("test'quote");
        Assert.AreEqual("'test'quote'", result);
    }

    /// <summary>
    /// Tests the WrapColumn method with special characters.
    /// </summary>
    [TestMethod]
    public void SqlDefine_WrapColumn_WithSpecialCharacters_HandlesCorrectly()
    {
        var result = SqlDefine.MySql.WrapColumn("test`backtick");
        Assert.AreEqual("`test`backtick`", result);
    }

    /// <summary>
    /// Tests that SqlDefine constants have correct parameter counts.
    /// </summary>
    [TestMethod]
    public void SqlDefine_Constants_HaveCorrectParameterCounts()
    {
        // Act & Assert
        var mysql = SqlDefine.MySql;
        var sqlServer = SqlDefine.SqlServer;
        var pgSql = SqlDefine.PgSql;

        // Test that all constants have 5 parameters
        Assert.AreEqual(5, mysql.GetType().GetConstructor(new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) })?.GetParameters().Length ?? 0);
        Assert.AreEqual(5, sqlServer.GetType().GetConstructor(new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) })?.GetParameters().Length ?? 0);
        Assert.AreEqual(5, pgSql.GetType().GetConstructor(new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) })?.GetParameters().Length ?? 0);
    }

    /// <summary>
    /// Tests that SqlDefine constants have correct parameter names.
    /// </summary>
    [TestMethod]
    public void SqlDefine_Constants_HaveCorrectParameterNames()
    {
        // Act & Assert
        var mysql = SqlDefine.MySql;
        var sqlServer = SqlDefine.SqlServer;
        var pgSql = SqlDefine.PgSql;

        // Test that all constants have the expected parameter names
        Assert.AreEqual("`", mysql.ColumnLeft);
        Assert.AreEqual("`", mysql.ColumnRight);
        Assert.AreEqual("'", mysql.StringLeft);
        Assert.AreEqual("'", mysql.StringRight);
        Assert.AreEqual("@", mysql.ParamterPrefx);

        Assert.AreEqual("[", sqlServer.ColumnLeft);
        Assert.AreEqual("]", sqlServer.ColumnRight);
        Assert.AreEqual("'", sqlServer.StringLeft);
        Assert.AreEqual("'", sqlServer.StringRight);
        Assert.AreEqual("@", sqlServer.ParamterPrefx);

        Assert.AreEqual("\"", pgSql.ColumnLeft);
        Assert.AreEqual("\"", pgSql.ColumnRight);
        Assert.AreEqual("'", pgSql.StringLeft);
        Assert.AreEqual("'", pgSql.StringRight);
        Assert.AreEqual("@", pgSql.ParamterPrefx);
    }

    /// <summary>
    /// Tests that SqlDefine constants are immutable.
    /// </summary>
    [TestMethod]
    public void SqlDefine_Constants_AreImmutable()
    {
        // Act & Assert
        var mysql = SqlDefine.MySql;
        var sqlServer = SqlDefine.SqlServer;
        var pgSql = SqlDefine.PgSql;

        // Test that the constants are read-only (immutable)
        Assert.IsTrue(mysql.GetType().IsValueType, "SqlDefine should be a value type (record)");
        
        // Test that the properties are read-only
        var properties = mysql.GetType().GetProperties();
        foreach (var property in properties)
        {
            Assert.IsTrue(property.CanRead, $"Property {property.Name} should be readable");
            Assert.IsFalse(property.CanWrite, $"Property {property.Name} should not be writable");
        }
    }

    /// <summary>
    /// Tests that SqlDefine constants have correct string representations.
    /// </summary>
    [TestMethod]
    public void SqlDefine_Constants_HaveCorrectStringRepresentations()
    {
        // Arrange
        var mysql = SqlDefine.MySql;
        var sqlServer = SqlDefine.SqlServer;
        var pgSql = SqlDefine.PgSql;

        // Act
        var mysqlString = mysql.ToString();
        var sqlServerString = sqlServer.ToString();
        var pgSqlString = pgSql.ToString();

        // Assert
        Assert.IsTrue(mysqlString.Contains("`"), "MySql string representation should contain backticks");
        Assert.IsTrue(sqlServerString.Contains("[") && sqlServerString.Contains("]"), "SqlServer string representation should contain brackets");
        Assert.IsTrue(pgSqlString.Contains("\""), "PgSql string representation should contain quotes");
    }

    /// <summary>
    /// Tests that SqlDefine constants can be used in pattern matching.
    /// </summary>
    [TestMethod]
    public void SqlDefine_Constants_CanBeUsedInPatternMatching()
    {
        // Act & Assert
        var mysql = SqlDefine.MySql;
        var sqlServer = SqlDefine.SqlServer;
        var pgSql = SqlDefine.PgSql;

        // Test pattern matching with deconstruction
        var (mysqlLeft, mysqlRight, mysqlStrLeft, mysqlStrRight, mysqlPrefix) = mysql;
        var (sqlServerLeft, sqlServerRight, sqlServerStrLeft, sqlServerStrRight, sqlServerPrefix) = sqlServer;
        var (pgSqlLeft, pgSqlRight, pgSqlStrLeft, pgSqlStrRight, pgSqlPrefix) = pgSql;

        Assert.AreEqual("`", mysqlLeft);
        Assert.AreEqual("`", mysqlRight);
        Assert.AreEqual("'", mysqlStrLeft);
        Assert.AreEqual("'", mysqlStrRight);
        Assert.AreEqual("@", mysqlPrefix);

        Assert.AreEqual("[", sqlServerLeft);
        Assert.AreEqual("]", sqlServerRight);
        Assert.AreEqual("'", sqlServerStrLeft);
        Assert.AreEqual("'", sqlServerStrRight);
        Assert.AreEqual("@", sqlServerPrefix);

        Assert.AreEqual("\"", pgSqlLeft);
        Assert.AreEqual("\"", pgSqlRight);
        Assert.AreEqual("'", pgSqlStrLeft);
        Assert.AreEqual("'", pgSqlStrRight);
        Assert.AreEqual("@", pgSqlPrefix);
    }

    /// <summary>
    /// Tests that SqlDefine constants have correct equality behavior.
    /// </summary>
    [TestMethod]
    public void SqlDefine_Constants_HaveCorrectEqualityBehavior()
    {
        // Act & Assert
        var mysql1 = SqlDefine.MySql;
        var mysql2 = SqlDefine.MySql;
        var sqlServer = SqlDefine.SqlServer;

        // Test that identical constants are equal
        Assert.AreEqual(mysql1, mysql2);
        Assert.IsTrue(mysql1.Equals(mysql2));

        // Test that different constants are not equal
        Assert.AreNotEqual(mysql1, sqlServer);
        Assert.IsFalse(mysql1.Equals(sqlServer));

        // Test that constants are not equal to null
        Assert.IsFalse(mysql1.Equals(null));
    }

    /// <summary>
    /// Tests that SqlDefine constants have correct hash code behavior.
    /// </summary>
    [TestMethod]
    public void SqlDefine_Constants_HaveCorrectHashCodeBehavior()
    {
        // Arrange
        var mysql1 = SqlDefine.MySql;
        var mysql2 = SqlDefine.MySql;
        var sqlServer = SqlDefine.SqlServer;

        // Act & Assert
        // Test that identical constants have the same hash code
        Assert.AreEqual(mysql1.GetHashCode(), mysql2.GetHashCode());

        // Test that different constants have different hash codes (usually)
        // Note: Hash code collisions are possible but unlikely
        Assert.AreNotEqual(mysql1.GetHashCode(), sqlServer.GetHashCode());
    }
}