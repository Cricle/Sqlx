// -----------------------------------------------------------------------
// <copyright file="MoqBestPractices.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sqlx.SqlGen;
using System;

/// <summary>
/// 展示在 Sqlx 项目中使用 Moq 的最佳实践和实际可行的用法。
/// </summary>
[TestClass]
public class MoqBestPractices
{
    /// <summary>
    /// ✅ 推荐：测试静态方法和实用函数 - 不需要 Mock。
    /// </summary>
    [TestMethod]
    public void RecommendedApproach_TestStaticMethods_DirectTesting()
    {
        // 测试 NameMapper - 静态方法，不需要依赖
        Assert.AreEqual("person_id", NameMapper.MapName("PersonId"));
        Assert.AreEqual("user_name", NameMapper.MapName("UserName"));
        
        // 测试 GenerateContext 的实用方法
        Assert.AreEqual("first_name", GenerateContext.GetColumnName("FirstName"));
        Assert.AreEqual("@user_id", GenerateContext.GetParamterName("@", "UserId"));
    }

    /// <summary>
    /// ✅ 推荐：测试具体类的行为 - 直接实例化测试。
    /// </summary>
    [TestMethod]
    public void RecommendedApproach_TestConcreteClasses_DirectInstantiation()
    {
        // 测试 IndentedStringBuilder
        var builder = new IndentedStringBuilder("Initial content");
        builder.AppendLine("Test line")
               .PushIndent()
               .AppendLine("Indented line")
               .PopIndent();

        var result = builder.ToString();
        StringAssert.Contains(result, "Test line");
        StringAssert.Contains(result, "Indented line");
    }

    /// <summary>
    /// ✅ 推荐：测试 SqlDefine 的不可变性和一致性。
    /// </summary>
    [TestMethod]
    public void RecommendedApproach_TestValueObjects_DirectComparison()
    {
        // 测试 SqlDefine 的各种方言
        Assert.AreEqual("[", SqlDefine.SqlServer.ColumnLeft);
        Assert.AreEqual("]", SqlDefine.SqlServer.ColumnRight);
        Assert.AreEqual("@", SqlDefine.SqlServer.ParamterPrefx);

        Assert.AreEqual("`", SqlDefine.MySql.ColumnLeft);
        Assert.AreEqual("`", SqlDefine.MySql.ColumnRight);

        Assert.AreEqual("\"", SqlDefine.PgSql.ColumnLeft);
        Assert.AreEqual("\"", SqlDefine.PgSql.ColumnRight);

        // 测试包装方法
        Assert.AreEqual("[table]", SqlDefine.SqlServer.WrapColumn("table"));
        Assert.AreEqual("`table`", SqlDefine.MySql.WrapColumn("table"));
        Assert.AreEqual("\"table\"", SqlDefine.PgSql.WrapColumn("table"));
    }

    /// <summary>
    /// ✅ 有限推荐：简单 Mock 用于简单接口。
    /// </summary>
    [TestMethod]
    public void LimitedRecommendation_SimpleMock_ForSimpleInterfaces()
    {
        // 简单的 Mock 可以用于验证基本的调用模式
        var mockLogger = new Mock<Action<string>>();
        
        // 模拟一个简单的日志记录场景
        mockLogger.Setup(log => log(It.IsAny<string>()));
        
        // 调用
        mockLogger.Object("Test message");
        
        // 验证调用
        mockLogger.Verify(log => log("Test message"), Times.Once);
    }

    /// <summary>
    /// ❌ 不推荐：Mock 复杂的 Roslyn API。
    /// </summary>
    [TestMethod]
    public void NotRecommended_ComplexMocking_ExplanationOnly()
    {
        // 这种做法不推荐，因为：
        // 1. Roslyn API 非常复杂，有很多内部依赖
        // 2. Mock 对象无法完全模拟真实的符号语义
        // 3. 测试变得脆弱且难以维护
        
        // 替代方案：
        // - 使用集成测试
        // - 测试更高层的逻辑
        // - 创建简单的测试数据而不是 Mock
        
        Assert.IsTrue(true, "This is just an explanation, not a real test");
    }

    /// <summary>
    /// ✅ 推荐的替代方案：测试 SQL 生成的核心逻辑。
    /// </summary>
    [TestMethod]
    public void AlternativeApproach_TestCoreLogic_WithoutComplexMocking()
    {
        // 直接测试 SqlGenerator 的简单案例
        var generator = new SqlGenerator();
        
        // 测试无效输入的处理
        var invalidType = (SqlExecuteTypes)999;
        var result = generator.Generate(SqlDefine.SqlServer, invalidType, null!);
        Assert.AreEqual(string.Empty, result);
        
        // 对于复杂的测试，应该使用集成测试而不是单元测试
        // 参见 samples/CompilationTests 中的实际使用示例
    }

    /// <summary>
    /// 📝 总结：何时使用 Moq 的指导原则。
    /// </summary>
    [TestMethod]
    public void Guidelines_WhenToUseMoq()
    {
        /*
         * ✅ 使用 Moq 的场景：
         * 1. 简单的接口或委托
         * 2. 验证方法调用次数和参数
         * 3. 隔离被测试代码的外部依赖
         * 4. 当依赖项很复杂但接口很简单时
         * 
         * ❌ 避免使用 Moq 的场景：
         * 1. 测试静态方法（直接测试即可）
         * 2. 测试具体类的简单方法
         * 3. 复杂的 API（如 Roslyn），Mock 无法完全模拟其行为
         * 4. 值对象或数据传输对象
         * 5. 当 Mock 设置比被测试的代码还复杂时
         * 
         * 🎯 在 Sqlx 项目中的具体建议：
         * - 测试 NameMapper, GenerateContext 等实用类：直接测试
         * - 测试 SqlDefine：直接比较值
         * - 测试 IndentedStringBuilder：直接实例化
         * - 测试代码生成：使用集成测试
         * - 复杂的 Roslyn 交互：在 samples 项目中验证
         */

        Assert.IsTrue(true, "Guidelines provided in comments");
    }
}

/// <summary>
/// 展示实际可行的测试简化方法的例子。
/// </summary>
[TestClass]
public class PracticalTestSimplification
{
    /// <summary>
    /// 使用测试数据而不是 Mock 来简化测试。
    /// </summary>
    [TestMethod]
    public void UseTestData_InsteadOfMocking()
    {
        // 创建简单的测试数据
        var testCases = new[]
        {
            new { Input = "PersonId", Expected = "person_id" },
            new { Input = "UserName", Expected = "user_name" },
            new { Input = "XMLParser", Expected = "x_m_l_parser" }
        };

        // 批量测试
        foreach (var testCase in testCases)
        {
            var result = NameMapper.MapName(testCase.Input);
            Assert.AreEqual(testCase.Expected, result, 
                $"NameMapper.MapName('{testCase.Input}') should return '{testCase.Expected}'");
        }
    }

    /// <summary>
    /// 使用参数化测试来减少重复代码。
    /// </summary>
    [TestMethod]
    [DataRow("PersonId", "person_id")]
    [DataRow("FirstName", "first_name")]
    [DataRow("XMLHttpRequest", "x_m_l_http_request")]
    [DataRow("ID", "i_d")]
    public void ParameterizedTest_ReducesDuplication(string input, string expected)
    {
        var result = NameMapper.MapName(input);
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// 使用 Helper 方法来构建测试数据。
    /// </summary>
    [TestMethod]
    public void UseHelperMethods_ToBuildTestData()
    {
        // Helper 方法比复杂的 Mock 设置更清晰
        var sqlDefines = GetAllSqlDefines();
        
        foreach (var define in sqlDefines)
        {
            var wrapped = define.SqlDefine.WrapColumn("test_table");
            Assert.IsTrue(wrapped.StartsWith(define.ExpectedLeft), 
                $"{define.Name} should start with {define.ExpectedLeft}");
            Assert.IsTrue(wrapped.EndsWith(define.ExpectedRight), 
                $"{define.Name} should end with {define.ExpectedRight}");
        }
    }

    /// <summary>
    /// Helper 方法：获取所有 SQL 方言定义。
    /// </summary>
    private static (string Name, SqlDefine SqlDefine, string ExpectedLeft, string ExpectedRight)[] GetAllSqlDefines()
    {
        return new[]
        {
            ("SQL Server", SqlDefine.SqlServer, "[", "]"),
            ("MySQL", SqlDefine.MySql, "`", "`"),
            ("PostgreSQL", SqlDefine.PgSql, "\"", "\"")
        };
    }
}
