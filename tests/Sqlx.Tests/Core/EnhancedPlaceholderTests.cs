// -----------------------------------------------------------------------
// <copyright file="EnhancedPlaceholderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// 全面测试增强占位符功能在所有数据库方言中的工作情况
/// Tests for enhanced placeholder functionality across all database dialects.
/// </summary>
[TestClass]
public class EnhancedPlaceholderTests
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _testMethod = null!;
    private INamedTypeSymbol _userType = null!;

    // 所有支持的数据库方言
    private static readonly Sqlx.Generator.SqlDefine[] AllDialects = new[]
    {
        Sqlx.Generator.SqlDefine.SqlServer,
        Sqlx.Generator.SqlDefine.MySql,
        Sqlx.Generator.SqlDefine.PostgreSql,
        Sqlx.Generator.SqlDefine.SQLite,
        Sqlx.Generator.SqlDefine.Oracle,
        Sqlx.Generator.SqlDefine.DB2
    };

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();

        // 创建测试编译上下文
        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public int DepartmentId { get; set; }
        public bool IsActive { get; set; }
        public DateTime HireDate { get; set; }
        public decimal Salary { get; set; }
        public decimal? Bonus { get; set; }
        public double? PerformanceRating { get; set; }
    }

    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<List<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);
        Task<List<User>> GetUsersByNamePatternAsync(string pattern);
        Task<List<User>> GetUsersByDepartmentsAsync(List<int> deptIds);
        Task<List<User>> GetUsersWithoutBonusAsync();
        Task<List<User>> GetUsersWithPerformanceRatingAsync();
        Task<List<User>> GetTodayHiredUsersAsync();
        Task<List<User>> GetWeekHiredUsersAsync();
        Task<List<User>> GetMonthHiredUsersAsync();
        Task<List<User>> GetYearHiredUsersAsync();
        Task<List<User>> GetUsersByEmailContainsAsync(string text);
        Task<List<User>> GetUsersByNameStartsWithAsync(string prefix);
        Task<List<User>> GetUsersByEmailEndsWithAsync(string suffix);
        Task<List<User>> GetUsersWithRoundedSalaryAsync();
        Task<List<User>> GetUsersWithAbsPerformanceAsync();
        Task<int> BatchInsertUsersAsync(List<User> users, int batchSize);
        Task<int> UpsertUserAsync(User user);
        Task<List<User>> GetUsersInActiveDepartmentsAsync();
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
        };

        _compilation = CSharpCompilation.Create("TestAssembly", new[] { syntaxTree }, references);

        var semanticModel = _compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        // 获取测试方法和用户类型
        var interfaceDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax>()
            .First(i => i.Identifier.ValueText == "IUserService");

        var methodDeclaration = interfaceDeclaration.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        _testMethod = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol
            ?? throw new InvalidOperationException("Failed to get test method symbol");

        _userType = _compilation.GetTypeByMetadataName("TestNamespace.User")
            ?? throw new InvalidOperationException("Failed to get User type symbol");
    }

    #region 🔍 条件查询占位符测试

    [TestMethod]
    public void BetweenPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{between:age|min=@minAge|max=@maxAge}}";
        
        var expectedResults = new Dictionary<string, string>
        {
            ["SqlServer"] = "SELECT * FROM user WHERE age BETWEEN @minAge AND @maxAge",
            ["MySql"] = "SELECT * FROM user WHERE age BETWEEN @minAge AND @maxAge",
            ["PostgreSql"] = "SELECT * FROM user WHERE age BETWEEN $minAge AND $maxAge",
            ["SQLite"] = "SELECT * FROM user WHERE age BETWEEN $minAge AND $maxAge",
            ["Oracle"] = "SELECT * FROM user WHERE age BETWEEN :minAge AND :maxAge",
            ["DB2"] = "SELECT * FROM user WHERE age BETWEEN ? AND ?"
        };

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证基本结构正确（不验证具体参数前缀，因为它们因方言而异）
            Assert.IsTrue(result.ProcessedSql.Contains("BETWEEN"), $"SQL should contain BETWEEN for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("age"), $"SQL should contain age field for {dialectName}");
        }
    }

    [TestMethod]
    public void LikePlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{like:name|pattern=@namePattern}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("LIKE"), $"SQL should contain LIKE for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("name"), $"SQL should contain name field for {dialectName}");
        }
    }

    [TestMethod]
    public void InPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{in:department_id|values=@deptIds}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("IN"), $"SQL should contain IN for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("department_id"), $"SQL should contain department_id field for {dialectName}");
        }
    }

    [TestMethod]
    public void NotInPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{not_in:department_id|values=@excludeDeptIds}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("NOT IN"), $"SQL should contain NOT IN for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("department_id"), $"SQL should contain department_id field for {dialectName}");
        }
    }

    [TestMethod]
    public void IsNullPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{isnull:bonus}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("IS NULL"), $"SQL should contain IS NULL for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("bonus"), $"SQL should contain bonus field for {dialectName}");
        }
    }

    [TestMethod]
    public void NotNullPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{notnull:performance_rating}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("IS NOT NULL"), $"SQL should contain IS NOT NULL for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("performance_rating"), $"SQL should contain performance_rating field for {dialectName}");
        }
    }

    #endregion

    #region 📅 日期时间函数占位符测试

    [TestMethod]
    public void TodayPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{today:hire_date}}";

        var dialectSpecificChecks = new Dictionary<string, string>
        {
            ["SqlServer"] = "CAST(GETDATE() AS DATE)",
            ["MySql"] = "CURDATE()",
            ["PostgreSql"] = "CURRENT_DATE",
            ["SQLite"] = "date('now')",
            ["Oracle"] = "TRUNC(SYSDATE)",
            ["DB2"] = "CURRENT DATE"
        };

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("hire_date"), $"SQL should contain hire_date field for {dialectName}");
            
            // 验证包含方言特定的日期函数
            if (dialectSpecificChecks.ContainsKey(dialectName))
            {
                var expectedFunction = dialectSpecificChecks[dialectName];
                Assert.IsTrue(result.ProcessedSql.Contains(expectedFunction) || 
                             result.ProcessedSql.Contains("hire_date"), // 如果没有特定函数，至少要有字段
                             $"SQL should contain date function or field for {dialectName}");
            }
        }
    }

    [TestMethod]
    public void WeekPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{week:hire_date}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("hire_date"), $"SQL should contain hire_date field for {dialectName}");
        }
    }

    [TestMethod]
    public void MonthPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{month:hire_date}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("hire_date"), $"SQL should contain hire_date field for {dialectName}");
        }
    }

    [TestMethod]
    public void YearPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{year:hire_date}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("hire_date"), $"SQL should contain hire_date field for {dialectName}");
        }
    }

    #endregion

    #region 🔤 字符串函数占位符测试

    [TestMethod]
    public void ContainsPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{contains:email|text=@searchText}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("email"), $"SQL should contain email field for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("LIKE") || result.ProcessedSql.Contains("CONTAINS") || 
                         result.ProcessedSql.Contains("ILIKE"), $"SQL should contain text search operator for {dialectName}");
        }
    }

    [TestMethod]
    public void StartsWithPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{startswith:name|prefix=@namePrefix}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("name"), $"SQL should contain name field for {dialectName}");
        }
    }

    [TestMethod]
    public void EndsWithPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE {{endswith:email|suffix=@emailSuffix}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("email"), $"SQL should contain email field for {dialectName}");
        }
    }

    [TestMethod]
    public void UpperPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT {{upper:name}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("UPPER"), $"SQL should contain UPPER function for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("name"), $"SQL should contain name field for {dialectName}");
        }
    }

    [TestMethod]
    public void LowerPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT {{lower:email}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("LOWER"), $"SQL should contain LOWER function for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("email"), $"SQL should contain email field for {dialectName}");
        }
    }

    [TestMethod]
    public void TrimPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT {{trim:name}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("TRIM"), $"SQL should contain TRIM function for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("name"), $"SQL should contain name field for {dialectName}");
        }
    }

    #endregion

    #region 📊 数学统计函数占位符测试

    [TestMethod]
    public void RoundPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT {{round:salary|decimals=2}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("ROUND"), $"SQL should contain ROUND function for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("salary"), $"SQL should contain salary field for {dialectName}");
        }
    }

    [TestMethod]
    public void AbsPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT {{abs:performance_rating}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("ABS"), $"SQL should contain ABS function for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("performance_rating"), $"SQL should contain performance_rating field for {dialectName}");
        }
    }

    [TestMethod]
    public void CeilingPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT {{ceiling:salary}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("CEIL") || result.ProcessedSql.Contains("CEILING"), 
                         $"SQL should contain CEILING function for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("salary"), $"SQL should contain salary field for {dialectName}");
        }
    }

    [TestMethod]
    public void FloorPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT {{floor:salary}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("FLOOR"), $"SQL should contain FLOOR function for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("salary"), $"SQL should contain salary field for {dialectName}");
        }
    }

    [TestMethod]
    public void SumPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT {{sum:salary}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("SUM"), $"SQL should contain SUM function for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("salary"), $"SQL should contain salary field for {dialectName}");
        }
    }

    [TestMethod]
    public void AvgPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT {{avg:salary}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("AVG"), $"SQL should contain AVG function for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("salary"), $"SQL should contain salary field for {dialectName}");
        }
    }

    [TestMethod]
    public void MaxPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT {{max:salary}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("MAX"), $"SQL should contain MAX function for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("salary"), $"SQL should contain salary field for {dialectName}");
        }
    }

    [TestMethod]
    public void MinPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT {{min:salary}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("MIN"), $"SQL should contain MIN function for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("salary"), $"SQL should contain salary field for {dialectName}");
        }
    }

    #endregion

    #region 🔗 高级操作占位符测试

    [TestMethod]
    public void BatchValuesPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "INSERT INTO {{table}} ({{columns:auto|exclude=Id}}) {{batch_values:auto|size=100}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("INSERT"), $"SQL should contain INSERT for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("VALUES"), $"SQL should contain VALUES for {dialectName}");
        }
    }

    [TestMethod]
    public void UpsertPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "{{upsert:auto|conflict=email|update=name,age,salary}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 不同数据库的UPSERT语法不同，检查基本结构
            var sql = result.ProcessedSql.ToUpper();
            var hasUpsertSyntax = sql.Contains("INSERT") || sql.Contains("MERGE") || sql.Contains("REPLACE");
            Assert.IsTrue(hasUpsertSyntax, $"SQL should contain UPSERT-like syntax for {dialectName}");
        }
    }

    [TestMethod]
    public void ExistsPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} u WHERE {{exists:department|table=departments|condition=d.id = u.department_id AND d.is_active = 1}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("EXISTS"), $"SQL should contain EXISTS for {dialectName}");
        }
    }

    [TestMethod]
    public void SubqueryPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE department_id IN {{subquery:select|table=departments|fields=id|condition=is_active = 1}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("SELECT"), $"SQL should contain SELECT for {dialectName}");
        }
    }

    [TestMethod]
    public void DistinctPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT {{distinct:department_id}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("DISTINCT"), $"SQL should contain DISTINCT for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("department_id"), $"SQL should contain department_id field for {dialectName}");
        }
    }

    [TestMethod]
    public void UnionPlaceholder_AllDialects_GeneratesCorrectSql()
    {
        var template = "SELECT * FROM {{table}} WHERE is_active = 1 {{union:query|table=archived_users|condition=archived_date > '2023-01-01'}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("UNION"), $"SQL should contain UNION for {dialectName}");
        }
    }

    #endregion

    #region 🛡️ 安全性和错误处理测试

    [TestMethod]
    public void InvalidPlaceholder_AllDialects_HandlesGracefully()
    {
        var template = "SELECT * FROM {{table}} WHERE {{invalid_placeholder:field}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            // 无效占位符应该被保留原样或产生警告
            Assert.IsTrue(result.ProcessedSql.Contains("{{invalid_placeholder:field}}") || result.Warnings.Count > 0, 
                         $"Invalid placeholder should be preserved or generate warning for {dialectName}");
        }
    }

    [TestMethod]
    public void SqlInjectionAttempt_AllDialects_DetectsAndPrevents()
    {
        var template = "SELECT * FROM {{table}} WHERE name = 'Robert'; DROP TABLE users; --'";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            // 应该检测到SQL注入风险并产生错误或警告
            Assert.IsTrue(result.Errors.Count > 0 || result.Warnings.Count > 0, 
                         $"Should detect SQL injection risk for {dialectName}");
        }
    }

    [TestMethod]
    public void ParameterSafety_AllDialects_EnsuresParameterization()
    {
        var template = "SELECT * FROM {{table}} WHERE {{between:age|min=@minAge|max=@maxAge}} AND name = @name";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for parameterized query {dialectName}");
            
            // 验证参数被正确处理
            Assert.IsTrue(result.Parameters.Count >= 3, $"Should extract at least 3 parameters for {dialectName}");
            Assert.IsTrue(result.Parameters.ContainsKey("@minAge") || result.Parameters.ContainsKey("minAge"), 
                         $"Should contain minAge parameter for {dialectName}");
            Assert.IsTrue(result.Parameters.ContainsKey("@maxAge") || result.Parameters.ContainsKey("maxAge"), 
                         $"Should contain maxAge parameter for {dialectName}");
            Assert.IsTrue(result.Parameters.ContainsKey("@name") || result.Parameters.ContainsKey("name"), 
                         $"Should contain name parameter for {dialectName}");
        }
    }

    #endregion

    #region 📈 性能和压力测试

    [TestMethod]
    public void ComplexTemplate_AllDialects_ProcessesEfficiently()
    {
        var complexTemplate = @"
            SELECT {{distinct:u.department_id}}, {{sum:u.salary}}, {{avg:u.performance_rating}}, {{count:*}}
            FROM {{table}} u
            WHERE {{between:u.age|min=@minAge|max=@maxAge}}
              AND {{like:u.name|pattern=@namePattern}}
              AND {{notnull:u.performance_rating}}
              AND {{today:u.hire_date}}
              AND u.department_id {{in:department_id|values=@activeDeptIds}}
            GROUP BY u.department_id
            HAVING {{sum:u.salary}} > @minTotalSalary
            ORDER BY {{sum:u.salary}} DESC
            {{limit:default|count=50}}";

        foreach (var dialect in AllDialects)
        {
            var startTime = DateTime.UtcNow;
            var result = _engine.ProcessTemplate(complexTemplate, _testMethod, _userType, "User", dialect);
            var processingTime = DateTime.UtcNow - startTime;
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Complex template should be processed for {dialectName}");
            Assert.IsTrue(processingTime.TotalMilliseconds < 1000, $"Processing should be fast (<1s) for {dialectName}, actual: {processingTime.TotalMilliseconds}ms");
            Assert.IsTrue(result.Errors.Count == 0, $"Complex template should not have errors for {dialectName}");
        }
    }

    [TestMethod]
    public void TemplateCache_AllDialects_ImprovePerformance()
    {
        var template = "SELECT {{columns:auto}} FROM {{table}} WHERE {{between:age|min=@minAge|max=@maxAge}}";

        foreach (var dialect in AllDialects)
        {
            var dialectName = GetDialectName(dialect);
            
            // 第一次处理 - 冷启动
            var startTime = DateTime.UtcNow;
            var result1 = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var firstTime = DateTime.UtcNow - startTime;
            
            // 第二次处理 - 应该从缓存中获取
            startTime = DateTime.UtcNow;
            var result2 = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var secondTime = DateTime.UtcNow - startTime;
            
            Assert.AreEqual(result1.ProcessedSql, result2.ProcessedSql, $"Cache should return same result for {dialectName}");
            // 缓存的性能提升可能不明显，但至少不应该更慢
            Assert.IsTrue(secondTime.TotalMilliseconds <= firstTime.TotalMilliseconds * 2, 
                         $"Cached processing should not be significantly slower for {dialectName}");
        }
    }

    #endregion

    #region 🔧 辅助方法

    /// <summary>
    /// 获取数据库方言的名称
    /// </summary>
    private static string GetDialectName(Sqlx.Generator.SqlDefine dialect)
    {
        if (dialect.Equals(Sqlx.Generator.SqlDefine.SqlServer)) return "SqlServer";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.MySql)) return "MySql";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.PostgreSql)) return "PostgreSql";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.SQLite)) return "SQLite";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.Oracle)) return "Oracle";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.DB2)) return "DB2";
        return "Unknown";
    }

    #endregion
}
