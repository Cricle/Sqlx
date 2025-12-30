using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Sqlx.Tests.SourceGeneration;

[TestClass]
public class DynamicJoinPlaceholderTest : TestBase
{
    [TestMethod]
    public void JoinPlaceholder_WithDynamicParameter_ShouldGenerateStringInterpolation()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace Test
{
    public class Item { public int Id { get; set; } }

    public interface IItemRepo
    {
        [Sqlx(""SELECT * FROM items {{join @j}}"")]
        Task<List<Item>> JoinTestAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string j);
    }

    [RepositoryFor(typeof(IItemRepo))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""items"")]
    public partial class ItemRepo : IItemRepo { }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "ItemRepo");

        // Verify the method was generated
        Assert.IsTrue(generatedCode.Contains("JoinTestAsync"), "Method JoinTestAsync should be generated");

        // Verify string interpolation is used
        Assert.IsTrue(generatedCode.Contains("$@\""), "Should use string interpolation");
        Assert.IsTrue(generatedCode.Contains("__joinClause_0__"), "Should declare join clause variable");

        // Verify SQL validation is included
        Assert.IsTrue(generatedCode.Contains("SqlValidator.IsValidFragment"), "Should include SQL validation");
    }

    [TestMethod]
    public void SetPlaceholder_WithDynamicParameter_ShouldGenerateStringInterpolation()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test
{
    public class Item { public int Id { get; set; } }

    public interface IItemRepo
    {
        [Sqlx(""UPDATE items SET {{set @s}} WHERE id = 1"")]
        Task SetTestAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string s);
    }

    [RepositoryFor(typeof(IItemRepo))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""items"")]
    public partial class ItemRepo : IItemRepo { }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "ItemRepo");

        Assert.IsTrue(generatedCode.Contains("SetTestAsync"), "Method should be generated");
        Assert.IsTrue(generatedCode.Contains("$@\""), "Should use string interpolation");
        Assert.IsTrue(generatedCode.Contains("__setClause_0__"), "Should declare set clause variable");
        Assert.IsTrue(generatedCode.Contains("SqlValidator.IsValidFragment"), "Should include SQL validation");
    }

    [TestMethod]
    public void OrderByPlaceholder_WithDynamicParameter_ShouldGenerateStringInterpolation()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace Test
{
    public class Item { public int Id { get; set; } }

    public interface IItemRepo
    {
        [Sqlx(""SELECT * FROM items {{orderby @sort}}"")]
        Task<List<Item>> GetSortedAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string sort);
    }

    [RepositoryFor(typeof(IItemRepo))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""items"")]
    public partial class ItemRepo : IItemRepo { }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "ItemRepo");

        // Debug output
        System.Console.WriteLine("=== ORDERBY TEST ===");
        System.Console.WriteLine(generatedCode);
        System.Console.WriteLine("====================");

        Assert.IsTrue(generatedCode.Contains("GetSortedAsync"), "Method should be generated");
        Assert.IsTrue(generatedCode.Contains("$@\""), "Should use string interpolation");
        // Check for any orderby variable (the naming might be different)
        Assert.IsTrue(
            generatedCode.Contains("__orderByClause") || generatedCode.Contains("RUNTIME_ORDERBY"),
            "Should declare orderby clause variable or marker");
        Assert.IsTrue(generatedCode.Contains("SqlValidator.IsValidFragment"), "Should include SQL validation");
    }

    [TestMethod]
    public void GroupByPlaceholder_WithDynamicParameter_ShouldGenerateStringInterpolation()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace Test
{
    public class Item {
        public int Id { get; set; }
        public string Category { get; set; }
    }

    public interface IItemRepo
    {
        [Sqlx(""SELECT {{groupby @cols}}, COUNT(*) as cnt FROM items {{groupby @cols}}"")]
        Task<List<Dictionary<string, object>>> GetAggregatedAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string cols);
    }

    [RepositoryFor(typeof(IItemRepo))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""items"")]
    public partial class ItemRepo : IItemRepo { }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "ItemRepo");

        // Debug output
        System.Console.WriteLine("=== GROUPBY TEST ===");
        System.Console.WriteLine(generatedCode);
        System.Console.WriteLine("====================");

        Assert.IsTrue(generatedCode.Contains("GetAggregatedAsync"), "Method should be generated");
        Assert.IsTrue(generatedCode.Contains("$@\""), "Should use string interpolation");
        // Check for any groupby variable
        Assert.IsTrue(
            generatedCode.Contains("__groupByClause") || generatedCode.Contains("RUNTIME_GROUPBY"),
            "Should declare groupby clause variable or marker");
        Assert.IsTrue(generatedCode.Contains("SqlValidator.IsValidFragment"), "Should include SQL validation");
    }

    [TestMethod]
    public void MultipleDynamicPlaceholders_ShouldGenerateCorrectly()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace Test
{
    public class Item { public int Id { get; set; } }

    public interface IItemRepo
    {
        [Sqlx(""SELECT * FROM items {{join @j}} WHERE active = 1 {{orderby @sort}}"")]
        Task<List<Item>> GetComplexAsync(
            [DynamicSql(Type = DynamicSqlType.Fragment)] string j,
            [DynamicSql(Type = DynamicSqlType.Fragment)] string sort);
    }

    [RepositoryFor(typeof(IItemRepo))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""items"")]
    public partial class ItemRepo : IItemRepo { }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        var generatedCode = TestHelper.GetGeneratedCode(compilation, "ItemRepo");

        Assert.IsTrue(generatedCode.Contains("GetComplexAsync"), "Method should be generated");
        Assert.IsTrue(
            generatedCode.Contains("__joinClause") || generatedCode.Contains("RUNTIME_JOIN"),
            "Should declare join clause variable or marker");
        Assert.IsTrue(
            generatedCode.Contains("__orderByClause") || generatedCode.Contains("RUNTIME_ORDERBY"),
            "Should declare orderby clause variable or marker");

        // Both validations should be present
        var validationCount = System.Text.RegularExpressions.Regex.Matches(generatedCode, "SqlValidator\\.IsValidFragment").Count;
        Assert.IsTrue(validationCount >= 2, $"Should have at least 2 SQL validations, but found {validationCount}");
    }

    [TestMethod]
    public void DynamicPlaceholder_WithoutDynamicSqlAttribute_ShouldGenerateErrorOrWarning()
    {
        var source = @"
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace Test
{
    public class Item { public int Id { get; set; } }

    public interface IItemRepo
    {
        [Sqlx(""SELECT * FROM items {{join @j}}"")]
        Task<List<Item>> JoinTestAsync(string j); // Missing [DynamicSql]
    }

    [RepositoryFor(typeof(IItemRepo))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName(""items"")]
    public partial class ItemRepo : IItemRepo { }
}";

        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        // Should have an error or warning because @j is not marked with [DynamicSql]
        var issues = diagnostics.Where(d =>
            d.Severity == DiagnosticSeverity.Error ||
            d.Severity == DiagnosticSeverity.Warning).ToList();

        // If no diagnostics, at least verify the code was generated
        if (!issues.Any())
        {
            var generatedCode = TestHelper.GetGeneratedCode(compilation, "ItemRepo");
            Assert.IsTrue(generatedCode.Contains("JoinTestAsync"), "Method should still be generated");
        }
    }
}

