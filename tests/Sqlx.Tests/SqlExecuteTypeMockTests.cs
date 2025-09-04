// -----------------------------------------------------------------------
// <copyright file="SqlExecuteTypeMockTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sqlx.SqlGen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

/// <summary>
/// 使用 Moq 简化的 SqlExecuteType 单元测试。
/// 专注于核心逻辑测试，不依赖复杂的代码生成。
/// </summary>
[TestClass]
public class SqlExecuteTypeMockTests
{
    /// <summary>
    /// 测试 SqlGenerator 的基本操作 - 使用简化的测试方式。
    /// </summary>
    [TestMethod]
    public void SqlGenerator_BasicOperations_WorkCorrectly()
    {
        // 这个测试专注于验证 SqlGenerator 的核心逻辑，
        // 而不依赖复杂的上下文对象
        var generator = new SqlGenerator();

        // 测试无效操作类型
        var invalidType = (SqlExecuteTypes)999;
        var result = generator.Generate(SqlDefine.SqlServer, invalidType, null!);
        Assert.AreEqual(string.Empty, result, "Invalid operation type should return empty string");
    }

    /// <summary>
    /// 测试 GenerateContext 的实用方法。
    /// </summary>
    [TestMethod]
    public void GenerateContext_UtilityMethods_WorkCorrectly()
    {
        // Test GetColumnName
        var testCases = new[]
        {
            new { Input = "PersonId", Expected = "person_id" },
            new { Input = "FirstName", Expected = "first_name" },
            new { Input = "XMLData", Expected = "x_m_l_data" },
            new { Input = "ID", Expected = "i_d" },
            new { Input = "simpletext", Expected = "simpletext" }
        };

        foreach (var testCase in testCases)
        {
            var result = GenerateContext.GetColumnName(testCase.Input);
            Assert.AreEqual(testCase.Expected, result, 
                $"GetColumnName('{testCase.Input}') should return '{testCase.Expected}'");
        }
    }

    /// <summary>
    /// 测试参数名生成。
    /// </summary>
    [TestMethod]
    public void GenerateContext_GetParameterName_GeneratesCorrectly()
    {
        // Test GetParamterName with different prefixes
        var testCases = new[]
        {
            new { Prefix = "@", Name = "PersonId", Expected = "@person_id" },
            new { Prefix = ":", Name = "FirstName", Expected = ":first_name" },
            new { Prefix = "$", Name = "ID", Expected = "$i_d" },
            new { Prefix = "", Name = "UserName", Expected = "user_name" }
        };

        foreach (var testCase in testCases)
        {
            var result = GenerateContext.GetParamterName(testCase.Prefix, testCase.Name);
            Assert.AreEqual(testCase.Expected, result,
                $"GetParamterName('{testCase.Prefix}', '{testCase.Name}') should return '{testCase.Expected}'");
        }
    }

    /// <summary>
    /// 测试 SqlDefine 的不可变性和一致性。
    /// </summary>
    [TestMethod]
    public void SqlDefine_StaticInstances_AreConsistent()
    {
        // Verify static instances exist and have expected properties
        Assert.IsNotNull(SqlDefine.SqlServer);
        Assert.IsNotNull(SqlDefine.MySql);
        Assert.IsNotNull(SqlDefine.PgSql);

        // Verify SQL Server settings
        Assert.AreEqual("[", SqlDefine.SqlServer.ColumnLeft);
        Assert.AreEqual("]", SqlDefine.SqlServer.ColumnRight);
        Assert.AreEqual("@", SqlDefine.SqlServer.ParamterPrefx);

        // Verify MySQL settings
        Assert.AreEqual("`", SqlDefine.MySql.ColumnLeft);
        Assert.AreEqual("`", SqlDefine.MySql.ColumnRight);
        Assert.AreEqual("@", SqlDefine.MySql.ParamterPrefx);

        // Verify PostgreSQL settings
        Assert.AreEqual("\"", SqlDefine.PgSql.ColumnLeft);
        Assert.AreEqual("\"", SqlDefine.PgSql.ColumnRight);
        Assert.AreEqual("@", SqlDefine.PgSql.ParamterPrefx);
    }


}

/// <summary>
/// 测试 SqlExecuteTypes 枚举的完整性。
/// </summary>
[TestClass]
public class SqlExecuteTypesEnumTests
{
    /// <summary>
    /// 验证枚举包含所有预期的值。
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_HasAllExpectedValues()
    {
        var expectedValues = new[]
        {
            SqlExecuteTypes.Select,
            SqlExecuteTypes.Insert,
            SqlExecuteTypes.Update,
            SqlExecuteTypes.Delete
        };

        foreach (var expectedValue in expectedValues)
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(SqlExecuteTypes), expectedValue),
                $"SqlExecuteTypes should define {expectedValue}");
        }
    }

    /// <summary>
    /// 验证枚举值的数值正确性。
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_HasCorrectNumericValues()
    {
        Assert.AreEqual(0, (int)SqlExecuteTypes.Select);
        Assert.AreEqual(1, (int)SqlExecuteTypes.Update);
        Assert.AreEqual(2, (int)SqlExecuteTypes.Insert);
        Assert.AreEqual(3, (int)SqlExecuteTypes.Delete);
    }

    /// <summary>
    /// 验证枚举可以正确转换为字符串和从字符串解析。
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_StringConversion_WorksCorrectly()
    {
        foreach (SqlExecuteTypes value in System.Enum.GetValues<SqlExecuteTypes>())
        {
            var stringValue = value.ToString();
            var parsedValue = System.Enum.Parse<SqlExecuteTypes>(stringValue);
            
            Assert.AreEqual(value, parsedValue, 
                $"Round-trip conversion should work for {value}");
        }
    }
}
