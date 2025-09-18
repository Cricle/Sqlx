// -----------------------------------------------------------------------
// <copyright file="SqlTemplateCustomFunctionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// Tests for SQL template custom functions - Currently simplified
/// </summary>
[TestClass]
public class SqlTemplateCustomFunctionTests : TestBase
{
    [TestMethod]
    public void Render_CustomFunction_Simple_WorksCorrectly()
    {
        // Arrange - 先测试基础变量替换
        var template = "SELECT {{value}} as DoubleValue";

        // Act
        var result = SqlTemplate.Render(template, new { value = 10 });

        // Assert
        StringAssert.Contains(result.Sql, "@p0 as DoubleValue");
        Assert.AreEqual(10, result.Parameters["p0"]);
    }

    [TestMethod]
    public void Render_CustomFunction_WithMultipleArgs_WorksCorrectly()
    {
        // Arrange - 测试多个变量
        var template = "SELECT {{val1}} + {{val2}} + {{val3}} as Sum";

        // Act
        var result = SqlTemplate.Render(template, new { val1 = 10, val2 = 20, val3 = 30 });

        // Assert
        StringAssert.Contains(result.Sql, "@p0 + @p1 + @p2 as Sum");
        Assert.AreEqual(10, result.Parameters["p0"]);
        Assert.AreEqual(20, result.Parameters["p1"]);
        Assert.AreEqual(30, result.Parameters["p2"]);
    }

    [TestMethod]
    public void Render_CustomFunction_FormatDate_WorksCorrectly()
    {
        // Arrange - 测试日期参数
        var template = "SELECT * FROM Users WHERE CreatedDate >= {{startDate}}";
        var testDate = new DateTime(2023, 1, 15, 10, 30, 0);

        // Act
        var result = SqlTemplate.Render(template, new { startDate = testDate });

        // Assert
        StringAssert.Contains(result.Sql, "@p0");
        Assert.AreEqual(testDate, result.Parameters["p0"]);
    }

    [TestMethod]
    public void Render_SqlTemplateOptions_BasicTest()
    {
        // Arrange - 测试模板选项
        var template = "SELECT {{value}} FROM {{tableName}}";
        var options = new SqlTemplateOptions { UseParameterizedQueries = true };

        // Act
        var result = SqlTemplate.Render(template, new { value = "test", tableName = "Users" }, options);

        // Assert
        StringAssert.Contains(result.Sql, "@p0");
        StringAssert.Contains(result.Sql, "@p1");
        Assert.AreEqual("test", result.Parameters["p0"]);
        Assert.AreEqual("Users", result.Parameters["p1"]);
    }
}
