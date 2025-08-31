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
}