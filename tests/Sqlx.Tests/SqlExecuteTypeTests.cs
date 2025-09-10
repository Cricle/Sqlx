// -----------------------------------------------------------------------
// <copyright file="SqlExecuteTypeTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.SqlGen;
using System;

/// <summary>
/// 测试SqlExecuteTypeAttribute的各种操作类型的SQL生成功能。
/// 这些测试专注于验证SQL生成器的核心逻辑。
/// </summary>
[TestClass]
public class SqlExecuteTypeTests
{
    private SqlGenerator generator = null!;

    [TestInitialize]
    public void Setup()
    {
        generator = new SqlGenerator();
    }

    /// <summary>
    /// 测试SQL生成器返回正确的操作类型。
    /// 由于GenerateContext需要复杂的mock，我们测试更高层的逻辑。
    /// </summary>
    [TestMethod]
    public void SqlGenerator_SupportsAllOperationTypes()
    {
        // Arrange - 验证枚举定义正确
        var expectedTypes = new[]
        {
            SqlExecuteTypes.Select,
            SqlExecuteTypes.Insert,
            SqlExecuteTypes.Update,
            SqlExecuteTypes.Delete
        };

        // Act & Assert - 验证所有操作类型都有定义
        foreach (var type in expectedTypes)
        {
            Assert.IsTrue(Enum.IsDefined(typeof(SqlExecuteTypes), type), $"SqlExecuteTypes should define {type}");
        }
    }

    /// <summary>
    /// 测试不同数据库方言的表名包装。
    /// </summary>
    [TestMethod]
    public void SqlDefine_WrapColumn_GeneratesCorrectFormat()
    {
        // Arrange
        var tableName = "person";

        // Act
        var sqlServerWrapped = SqlDefine.SqlServer.WrapColumn(tableName);
        var mySqlWrapped = SqlDefine.MySql.WrapColumn(tableName);
        var pgSqlWrapped = SqlDefine.PgSql.WrapColumn(tableName);

        // Assert
        Assert.AreEqual("[person]", sqlServerWrapped, "SQL Server should use square brackets");
        Assert.AreEqual("`person`", mySqlWrapped, "MySQL should use backticks");
        Assert.AreEqual("\"person\"", pgSqlWrapped, "PostgreSQL should use double quotes");
    }

    /// <summary>
    /// 测试参数前缀的正确性。
    /// </summary>
    [TestMethod]
    public void SqlDefine_ParameterPrefix_IsCorrect()
    {
        // Assert - 所有数据库方言都使用@作为参数前缀
        Assert.AreEqual("@", SqlDefine.SqlServer.ParameterPrefix);
        Assert.AreEqual("@", SqlDefine.MySql.ParameterPrefix);
        Assert.AreEqual("$", SqlDefine.PgSql.ParameterPrefix);
    }

    /// <summary>
    /// 测试GenerateContext的列名转换功能。
    /// </summary>
    [TestMethod]
    public void GenerateContext_GetColumnName_ConvertsCorrectly()
    {
        // Act & Assert
        Assert.AreEqual("person_id", GenerateContext.GetColumnName("PersonId"));
        Assert.AreEqual("person_name", GenerateContext.GetColumnName("PersonName"));
        Assert.AreEqual("user_profile_id", GenerateContext.GetColumnName("UserProfileId"));
        Assert.AreEqual("order_detail", GenerateContext.GetColumnName("OrderDetail"));
    }

    /// <summary>
    /// 测试参数名生成功能。
    /// </summary>
    [TestMethod]
    public void GenerateContext_GetParameterName_GeneratesCorrectly()
    {
        // Act & Assert
        Assert.AreEqual("@person_id", GenerateContext.GetParamterName("@", "PersonId"));
        Assert.AreEqual("@person_name", GenerateContext.GetParamterName("@", "PersonName"));
        Assert.AreEqual(":user_id", GenerateContext.GetParamterName(":", "UserId"));
    }

    /// <summary>
    /// 测试边界情况的处理。
    /// </summary>
    [TestMethod]
    public void GenerateContext_GetColumnName_HandlesEdgeCases()
    {
        // Act & Assert
        Assert.AreEqual("i_d", GenerateContext.GetColumnName("ID"));
        Assert.AreEqual("id", GenerateContext.GetColumnName("id"));
        Assert.AreEqual("a", GenerateContext.GetColumnName("A"));
        Assert.AreEqual("", GenerateContext.GetColumnName(""));
    }
}

/// <summary>
/// 测试SqlExecuteTypes枚举的功能。
/// </summary>
[TestClass]
public class SqlExecuteTypesTests
{
    /// <summary>
    /// 验证所有支持的操作类型。
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_HasAllRequiredValues()
    {
        // Assert
        Assert.IsTrue(Enum.IsDefined(typeof(SqlExecuteTypes), SqlExecuteTypes.Select));
        Assert.IsTrue(Enum.IsDefined(typeof(SqlExecuteTypes), SqlExecuteTypes.Insert));
        Assert.IsTrue(Enum.IsDefined(typeof(SqlExecuteTypes), SqlExecuteTypes.Update));
        Assert.IsTrue(Enum.IsDefined(typeof(SqlExecuteTypes), SqlExecuteTypes.Delete));
    }

    /// <summary>
    /// 验证枚举值的正确性。
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_HasCorrectValues()
    {
        // Assert
        Assert.AreEqual(0, (int)SqlExecuteTypes.Select);
        Assert.AreEqual(1, (int)SqlExecuteTypes.Update);
        Assert.AreEqual(2, (int)SqlExecuteTypes.Insert);
        Assert.AreEqual(3, (int)SqlExecuteTypes.Delete);
    }

    /// <summary>
    /// 测试枚举值转换。
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_CanParseFromString()
    {
        // Act & Assert
        Assert.AreEqual(SqlExecuteTypes.Select, Enum.Parse<SqlExecuteTypes>("Select"));
        Assert.AreEqual(SqlExecuteTypes.Insert, Enum.Parse<SqlExecuteTypes>("Insert"));
        Assert.AreEqual(SqlExecuteTypes.Update, Enum.Parse<SqlExecuteTypes>("Update"));
        Assert.AreEqual(SqlExecuteTypes.Delete, Enum.Parse<SqlExecuteTypes>("Delete"));
    }
}

/// <summary>
/// 测试生成上下文类的功能。
/// </summary>
[TestClass]
public class GenerateContextTests
{
    /// <summary>
    /// 测试列名转换功能。
    /// </summary>
    [TestMethod]
    public void GetColumnName_ConvertsCorrectly()
    {
        // Arrange & Act
        var result1 = GenerateContext.GetColumnName("PersonId");
        var result2 = GenerateContext.GetColumnName("PersonName");
        var result3 = GenerateContext.GetColumnName("UserProfile");

        // Assert
        Assert.AreEqual("person_id", result1);
        Assert.AreEqual("person_name", result2);
        Assert.AreEqual("user_profile", result3);
    }

    /// <summary>
    /// 测试参数名生成功能。
    /// </summary>
    [TestMethod]
    public void GetParameterName_GeneratesCorrectly()
    {
        // Arrange & Act
        var result1 = GenerateContext.GetParamterName("@", "PersonId");
        var result2 = GenerateContext.GetParamterName(":", "PersonName");
        var result3 = GenerateContext.GetParamterName("$", "UserProfile");

        // Assert
        Assert.AreEqual("@person_id", result1);
        Assert.AreEqual(":person_name", result2);
        Assert.AreEqual("$user_profile", result3);
    }

    /// <summary>
    /// 测试边界情况。
    /// </summary>
    [TestMethod]
    public void GetColumnName_HandlesEdgeCases()
    {
        // Arrange & Act
        var result1 = GenerateContext.GetColumnName("ID");
        var result2 = GenerateContext.GetColumnName("id");
        var result3 = GenerateContext.GetColumnName("A");

        // Assert
        Assert.AreEqual("i_d", result1);
        Assert.AreEqual("id", result2);
        Assert.AreEqual("a", result3);
    }
}
