// -----------------------------------------------------------------------
// <copyright file="PlaceholderPerformanceTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlx.Tests.Core;

/// <summary>
/// 占位符性能和压力测试
/// Performance and stress tests for placeholder functionality.
/// </summary>
[TestClass]
public class PlaceholderPerformanceTests
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _testMethod = null!;
    private INamedTypeSymbol _userType = null!;

    // 测试用的数据库方言
    private static readonly Sqlx.Generator.SqlDefine[] TestDialects = new[]
    {
        Sqlx.Generator.SqlDefine.SqlServer,
        Sqlx.Generator.SqlDefine.MySql,
        Sqlx.Generator.SqlDefine.PostgreSql,
        Sqlx.Generator.SqlDefine.SQLite
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
        public string Address { get; set; }
        public string Phone { get; set; }
        public DateTime LastLoginDate { get; set; }
        public int LoginCount { get; set; }
        public string Preferences { get; set; }
    }

    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<List<User>> GetUsersByComplexCriteriaAsync(int minAge, int maxAge, string namePattern, List<int> deptIds, decimal minSalary);
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

    #region ⚡ 基础性能测试

    [TestMethod]
    public void SimpleTemplate_Performance_ProcessesFast()
    {
        var template = "SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}";
        var iterations = 1000;

        foreach (var dialect in TestDialects)
        {
            var dialectName = GetDialectName(dialect);
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Result should not be empty for {dialectName}");
            }

            stopwatch.Stop();
            var avgTime = stopwatch.ElapsedMilliseconds / (double)iterations;

            Assert.IsTrue(avgTime < 5, $"Average processing time should be < 5ms for {dialectName}, actual: {avgTime:F2}ms");
            Console.WriteLine($"[{dialectName}] Simple template: {avgTime:F2}ms average over {iterations} iterations");
        }
    }

    [TestMethod]
    public void ComplexTemplate_Performance_ProcessesReasonablyFast()
    {
        var complexTemplate = @"
            SELECT {{distinct:u.department_id}}, {{sum:u.salary}}, {{avg:u.performance_rating}}, {{count:*}}
            FROM {{table}} u
            WHERE {{between:u.age|min=@minAge|max=@maxAge}}
              AND {{like:u.name|pattern=@namePattern}}
              AND {{notnull:u.performance_rating}}
              AND {{today:u.hire_date}}
              AND u.department_id {{in:department_id|values=@activeDeptIds}}
              AND {{contains:u.email|text=@emailDomain}}
              AND {{round:u.salary|decimals=2}} > @minSalary
            GROUP BY u.department_id
            HAVING {{sum:u.salary}} > @minTotalSalary
            ORDER BY {{sum:u.salary}} DESC, {{max:u.performance_rating}} DESC
            {{limit:default|count=50}}";

        var iterations = 100;

        foreach (var dialect in TestDialects)
        {
            var dialectName = GetDialectName(dialect);
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var result = _engine.ProcessTemplate(complexTemplate, _testMethod, _userType, "User", dialect);
                Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Result should not be empty for {dialectName}");
                Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            }

            stopwatch.Stop();
            var avgTime = stopwatch.ElapsedMilliseconds / (double)iterations;

            Assert.IsTrue(avgTime < 50, $"Complex template processing should be < 50ms for {dialectName}, actual: {avgTime:F2}ms");
            Console.WriteLine($"[{dialectName}] Complex template: {avgTime:F2}ms average over {iterations} iterations");
        }
    }

    #endregion

    #region 🔄 缓存性能测试

    [TestMethod]
    public void Template_Cache_ImprovesPerformance()
    {
        var template = "SELECT {{columns:auto|exclude=Password}} FROM {{table}} WHERE {{between:age|min=@minAge|max=@maxAge}} AND {{like:name|pattern=@pattern}}";
        var coldRuns = 10;
        var warmRuns = 100;

        foreach (var dialect in TestDialects)
        {
            var dialectName = GetDialectName(dialect);
            
            // 冷启动测试
            var coldStopwatch = Stopwatch.StartNew();
            for (int i = 0; i < coldRuns; i++)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
            }
            coldStopwatch.Stop();
            var coldAvg = coldStopwatch.ElapsedMilliseconds / (double)coldRuns;

            // 预热缓存后测试
            var warmStopwatch = Stopwatch.StartNew();
            for (int i = 0; i < warmRuns; i++)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
            }
            warmStopwatch.Stop();
            var warmAvg = warmStopwatch.ElapsedMilliseconds / (double)warmRuns;

            // 缓存后的性能应该至少不更差
            Assert.IsTrue(warmAvg <= coldAvg * 1.5, $"Cached performance should not be significantly worse for {dialectName}");
            
            Console.WriteLine($"[{dialectName}] Cache test - Cold: {coldAvg:F2}ms, Warm: {warmAvg:F2}ms");
        }
    }

    [TestMethod]
    public void DifferentTemplates_Cache_HandlesCorrectly()
    {
        var templates = new[]
        {
            "SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}",
            "UPDATE {{table}} SET {{set:auto}} WHERE {{where:id}}",
            "INSERT INTO {{table}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})",
            "DELETE FROM {{table}} WHERE {{where:id}}",
            "SELECT {{count:*}} FROM {{table}} WHERE {{between:age|min=@min|max=@max}}"
        };

        var iterations = 50;

        foreach (var dialect in TestDialects)
        {
            var dialectName = GetDialectName(dialect);
            var stopwatch = Stopwatch.StartNew();
            var results = new List<string>();

            for (int i = 0; i < iterations; i++)
            {
                foreach (var template in templates)
                {
                    var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                    results.Add(result.ProcessedSql);
                }
            }

            stopwatch.Stop();
            var avgTime = stopwatch.ElapsedMilliseconds / (double)(iterations * templates.Length);

            // 验证每个模板产生不同结果
            var uniqueResults = results.Distinct().Count();
            Assert.IsTrue(uniqueResults >= templates.Length, $"Should generate distinct results for different templates for {dialectName}");
            
            Assert.IsTrue(avgTime < 10, $"Multi-template processing should be fast for {dialectName}, actual: {avgTime:F2}ms");
            Console.WriteLine($"[{dialectName}] Multi-template cache: {avgTime:F2}ms average per template");
        }
    }

    #endregion

    #region 🧵 并发性能测试

    [TestMethod]
    public void ConcurrentProcessing_Performance_IsThreadSafe()
    {
        var template = "SELECT {{columns:auto}} FROM {{table}} WHERE {{between:age|min=@minAge|max=@maxAge}} AND {{like:name|pattern=@pattern}}";
        var concurrentTasks = 10;
        var iterationsPerTask = 50;

        foreach (var dialect in TestDialects)
        {
            var dialectName = GetDialectName(dialect);
            var results = new ConcurrentBag<string>();
            var errors = new ConcurrentBag<Exception>();
            var stopwatch = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, concurrentTasks).Select(taskId => Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < iterationsPerTask; i++)
                    {
                        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                        results.Add(result.ProcessedSql);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }));

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            Assert.AreEqual(0, errors.Count, $"No exceptions should occur during concurrent processing for {dialectName}");
            Assert.AreEqual(concurrentTasks * iterationsPerTask, results.Count, $"All results should be collected for {dialectName}");

            // 验证所有结果都相同（相同模板应产生相同结果）
            var uniqueResults = results.Distinct().Count();
            Assert.AreEqual(1, uniqueResults, $"All concurrent results should be identical for {dialectName}");

            var avgTime = stopwatch.ElapsedMilliseconds / (double)(concurrentTasks * iterationsPerTask);
            Assert.IsTrue(avgTime < 20, $"Concurrent processing should be reasonably fast for {dialectName}, actual: {avgTime:F2}ms");
            
            Console.WriteLine($"[{dialectName}] Concurrent test: {avgTime:F2}ms average, {concurrentTasks} tasks × {iterationsPerTask} iterations");
        }
    }

    [TestMethod]
    public void HighLoad_Processing_MaintainsPerformance()
    {
        var templates = new[]
        {
            "SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}",
            "SELECT {{sum:salary}}, {{avg:age}} FROM {{table}} WHERE {{between:age|min=@min|max=@max}}",
            "UPDATE {{table}} SET {{set:auto}} WHERE {{where:id}}",
            "INSERT INTO {{table}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})",
            "SELECT {{distinct:department_id}} FROM {{table}} WHERE {{notnull:bonus}}"
        };

        var highLoadIterations = 500;
        var concurrentTasks = 5;

        foreach (var dialect in TestDialects)
        {
            var dialectName = GetDialectName(dialect);
            var allResults = new ConcurrentBag<(string Template, string Result, long ElapsedMs)>();
            var stopwatch = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, concurrentTasks).Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < highLoadIterations; i++)
                {
                    var template = templates[i % templates.Length];
                    var taskStopwatch = Stopwatch.StartNew();
                    
                    var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                    
                    taskStopwatch.Stop();
                    allResults.Add((template, result.ProcessedSql, taskStopwatch.ElapsedMilliseconds));
                    
                    Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Result should not be empty for {dialectName}");
                }
            }));

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            var totalOperations = concurrentTasks * highLoadIterations;
            var avgTime = stopwatch.ElapsedMilliseconds / (double)totalOperations;
            var maxTime = allResults.Max(r => r.ElapsedMs);
            var minTime = allResults.Min(r => r.ElapsedMs);

            Assert.IsTrue(avgTime < 15, $"High load average time should be reasonable for {dialectName}, actual: {avgTime:F2}ms");
            Assert.IsTrue(maxTime < 100, $"No single operation should be too slow for {dialectName}, max: {maxTime}ms");
            
            Console.WriteLine($"[{dialectName}] High load test: {avgTime:F2}ms avg, {minTime}ms min, {maxTime}ms max over {totalOperations} operations");
        }
    }

    #endregion

    #region 📊 内存和资源测试

    [TestMethod]
    public void LargeTemplate_Memory_HandlesEfficiently()
    {
        // 创建一个包含大量占位符的模板
        var largePlaceholders = new[]
        {
            "{{columns:auto}}", "{{where:auto}}", "{{orderby:id}}", "{{limit:default|count=100}}",
            "{{sum:salary}}", "{{avg:age}}", "{{max:performance_rating}}", "{{min:hire_date}}",
            "{{upper:name}}", "{{lower:email}}", "{{trim:address}}", "{{round:salary|decimals=2}}",
            "{{between:age|min=@minAge|max=@maxAge}}", "{{like:name|pattern=@pattern}}",
            "{{in:department_id|values=@deptIds}}", "{{notnull:bonus}}", "{{today:hire_date}}"
        };

        var largeTemplate = $@"
            SELECT {string.Join(", ", largePlaceholders.Take(8))}
            FROM {{table}} u
            WHERE {string.Join(" AND ", largePlaceholders.Skip(8).Take(6))}
            GROUP BY {largePlaceholders[0]}
            HAVING {largePlaceholders[4]} > @threshold
            ORDER BY {largePlaceholders[5]} DESC
            {largePlaceholders[3]}";

        foreach (var dialect in TestDialects)
        {
            var dialectName = GetDialectName(dialect);
            
            // 监控内存使用
            var initialMemory = GC.GetTotalMemory(true);
            var stopwatch = Stopwatch.StartNew();

            var result = _engine.ProcessTemplate(largeTemplate, _testMethod, _userType, "User", dialect);

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Large template should be processed for {dialectName}");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, $"Large template processing should be fast for {dialectName}");
            Assert.IsTrue(memoryIncrease < 1024 * 1024, $"Memory increase should be reasonable for {dialectName}, actual: {memoryIncrease / 1024}KB");

            Console.WriteLine($"[{dialectName}] Large template: {stopwatch.ElapsedMilliseconds}ms, {memoryIncrease / 1024}KB memory");
        }
    }

    [TestMethod]
    public void RepeatedProcessing_Memory_DoesNotLeak()
    {
        var template = "SELECT {{columns:auto}} FROM {{table}} WHERE {{between:age|min=@minAge|max=@maxAge}} AND {{like:name|pattern=@pattern}}";
        var iterations = 1000;

        foreach (var dialect in TestDialects)
        {
            var dialectName = GetDialectName(dialect);
            
            // 第一次测量基线内存
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var baselineMemory = GC.GetTotalMemory(false);

            // 执行大量操作
            for (int i = 0; i < iterations; i++)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
            }

            // 强制垃圾回收并测量最终内存
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - baselineMemory;

            // 内存增长应该在合理范围内（允许一些缓存）
            Assert.IsTrue(memoryIncrease < 1024 * 1024, $"Memory leak test failed for {dialectName}, increase: {memoryIncrease / 1024}KB");
            
            Console.WriteLine($"[{dialectName}] Memory leak test: {memoryIncrease / 1024}KB increase after {iterations} iterations");
        }
    }

    #endregion

    #region 🎯 特定占位符性能测试

    [TestMethod]
    public void DateTimePlaceholders_Performance_AreOptimized()
    {
        var dateTemplates = new[]
        {
            "SELECT * FROM {{table}} WHERE {{today:hire_date}}",
            "SELECT * FROM {{table}} WHERE {{week:hire_date}}",
            "SELECT * FROM {{table}} WHERE {{month:hire_date}}",
            "SELECT * FROM {{table}} WHERE {{year:hire_date}}"
        };

        var iterations = 200;

        foreach (var dialect in TestDialects)
        {
            var dialectName = GetDialectName(dialect);
            var totalTime = 0L;

            foreach (var template in dateTemplates)
            {
                var stopwatch = Stopwatch.StartNew();
                
                for (int i = 0; i < iterations; i++)
                {
                    var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                    Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
                }
                
                stopwatch.Stop();
                totalTime += stopwatch.ElapsedMilliseconds;
            }

            var avgTime = totalTime / (double)(dateTemplates.Length * iterations);
            Assert.IsTrue(avgTime < 8, $"DateTime placeholders should be fast for {dialectName}, actual: {avgTime:F2}ms");
            
            Console.WriteLine($"[{dialectName}] DateTime placeholders: {avgTime:F2}ms average");
        }
    }

    [TestMethod]
    public void StringFunctionPlaceholders_Performance_AreOptimized()
    {
        var stringTemplates = new[]
        {
            "SELECT {{upper:name}} FROM {{table}}",
            "SELECT {{lower:email}} FROM {{table}}",
            "SELECT {{trim:address}} FROM {{table}}",
            "SELECT * FROM {{table}} WHERE {{contains:email|text=@domain}}",
            "SELECT * FROM {{table}} WHERE {{startswith:name|prefix=@prefix}}",
            "SELECT * FROM {{table}} WHERE {{endswith:email|suffix=@suffix}}"
        };

        var iterations = 200;

        foreach (var dialect in TestDialects)
        {
            var dialectName = GetDialectName(dialect);
            var totalTime = 0L;

            foreach (var template in stringTemplates)
            {
                var stopwatch = Stopwatch.StartNew();
                
                for (int i = 0; i < iterations; i++)
                {
                    var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                    Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
                }
                
                stopwatch.Stop();
                totalTime += stopwatch.ElapsedMilliseconds;
            }

            var avgTime = totalTime / (double)(stringTemplates.Length * iterations);
            Assert.IsTrue(avgTime < 8, $"String function placeholders should be fast for {dialectName}, actual: {avgTime:F2}ms");
            
            Console.WriteLine($"[{dialectName}] String function placeholders: {avgTime:F2}ms average");
        }
    }

    [TestMethod]
    public void MathFunctionPlaceholders_Performance_AreOptimized()
    {
        var mathTemplates = new[]
        {
            "SELECT {{round:salary|decimals=2}} FROM {{table}}",
            "SELECT {{abs:performance_rating}} FROM {{table}}",
            "SELECT {{ceiling:salary}} FROM {{table}}",
            "SELECT {{floor:salary}} FROM {{table}}",
            "SELECT {{sum:salary}} FROM {{table}}",
            "SELECT {{avg:age}} FROM {{table}}",
            "SELECT {{max:salary}} FROM {{table}}",
            "SELECT {{min:age}} FROM {{table}}"
        };

        var iterations = 200;

        foreach (var dialect in TestDialects)
        {
            var dialectName = GetDialectName(dialect);
            var totalTime = 0L;

            foreach (var template in mathTemplates)
            {
                var stopwatch = Stopwatch.StartNew();
                
                for (int i = 0; i < iterations; i++)
                {
                    var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                    Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
                }
                
                stopwatch.Stop();
                totalTime += stopwatch.ElapsedMilliseconds;
            }

            var avgTime = totalTime / (double)(mathTemplates.Length * iterations);
            Assert.IsTrue(avgTime < 8, $"Math function placeholders should be fast for {dialectName}, actual: {avgTime:F2}ms");
            
            Console.WriteLine($"[{dialectName}] Math function placeholders: {avgTime:F2}ms average");
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
        return "Unknown";
    }

    #endregion
}
