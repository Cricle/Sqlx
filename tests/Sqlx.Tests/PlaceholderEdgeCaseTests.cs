// <copyright file="PlaceholderEdgeCaseTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// 测试占位符生成的边界情况和潜在问题.
/// </summary>
[TestClass]
public class PlaceholderEdgeCaseTests
{
    #region 逗号处理边界情况

    [TestMethod]
    public void InlineExpression_MultipleCommasInFunction_ParsesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("value", "Value", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        
        // SUBSTRING 函数有多个逗号参数
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Value=SUBSTRING(Value,1,10)}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        Assert.IsTrue(sql.Contains("SUBSTRING([value],1,10)"), "应该保留函数内的所有逗号");
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        
        Console.WriteLine($"SUBSTRING SQL: {sql}");
    }

    [TestMethod]
    public void InlineExpression_NestedFunctionsWithCommas_ParsesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("result", "Result", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        
        // 嵌套函数，每个都有逗号
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Result=COALESCE(NULLIF(Result,''),SUBSTRING('default',1,7))}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        Assert.IsTrue(sql.Contains("COALESCE(NULLIF([result],''),SUBSTRING('default',1,7))"), "应该保留所有嵌套函数的逗号");
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        
        Console.WriteLine($"嵌套函数 SQL: {sql}");
    }

    [TestMethod]
    public void InlineExpression_MultipleExpressionsWithComplexFunctions_ParsesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("field1", "Field1", DbType.String, false),
            new ColumnMeta("field2", "Field2", DbType.String, false),
            new ColumnMeta("field3", "Field3", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        
        // 多个表达式，每个都有复杂的函数
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Field1=COALESCE(Field1,'default'),Field2=SUBSTRING(Field2,1,10),Field3=CASE WHEN Field3>10 THEN 10 ELSE Field3 END}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        Assert.IsTrue(sql.Contains("COALESCE([field1],'default')"), "第一个表达式应该正确");
        Assert.IsTrue(sql.Contains("SUBSTRING([field2],1,10)"), "第二个表达式应该正确");
        Assert.IsTrue(sql.Contains("CASE WHEN [field3]>10 THEN 10 ELSE [field3] END"), "第三个表达式应该正确");
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        
        Console.WriteLine($"多表达式 SQL: {sql}");
    }

    #endregion

    #region 引号处理边界情况

    [TestMethod]
    public void InlineExpression_SingleQuoteInString_PreservesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("message", "Message", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Message='It''s working'}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        Assert.IsTrue(sql.Contains("'It''s working'"), "应该保留转义的单引号");
        
        Console.WriteLine($"单引号 SQL: {sql}");
    }

    [TestMethod]
    public void InlineExpression_DoubleQuoteInString_PreservesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("json_data", "JsonData", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline JsonData='{\"key\":\"value\"}'}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        Assert.IsTrue(sql.Contains("{\"key\":\"value\"}"), "应该保留 JSON 字符串中的双引号");
        
        Console.WriteLine($"双引号 SQL: {sql}");
    }

    #endregion

    #region 括号嵌套边界情况

    [TestMethod]
    public void InlineExpression_DeeplyNestedParentheses_ParsesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("result", "Result", DbType.Decimal, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Result=(((Result+@a)*@b)/@c)+@d}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        Assert.IsTrue(sql.Contains("((([result]+@a)*@b)/@c)+@d"), "应该保留所有嵌套括号");
        
        Console.WriteLine($"深度嵌套括号 SQL: {sql}");
    }

    [TestMethod]
    public void InlineExpression_MixedParenthesesAndFunctions_ParsesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("value", "Value", DbType.Decimal, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Value=ROUND((Value*1.1)+COALESCE(@discount,0),2)}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        Assert.IsTrue(sql.Contains("ROUND(([value]*1.1)+COALESCE(@discount,0),2)"), "应该正确处理混合的括号和函数");
        
        Console.WriteLine($"混合括号函数 SQL: {sql}");
    }

    #endregion

    #region 空格和格式化边界情况

    [TestMethod]
    public void InlineExpression_ExtraSpaces_HandlesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("value", "Value", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Value = Value + 1}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        Assert.IsTrue(sql.Contains("[value] = [value] + 1"), "应该保留表达式中的空格");
        
        Console.WriteLine($"额外空格 SQL: {sql}");
    }

    [TestMethod]
    public void InlineExpression_NoSpaces_HandlesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("value", "Value", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Value=Value+1}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        // 表达式中的空格会被保留，所以检查核心内容
        Assert.IsTrue(sql.Contains("[value] = [value]+1") || sql.Contains("[value]=[value]+1"), "应该包含正确的表达式");
        
        Console.WriteLine($"无空格 SQL: {sql}");
    }

    #endregion

    #region 特殊字符边界情况

    [TestMethod]
    public void InlineExpression_WithOperators_ParsesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("value", "Value", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Value=Value*2+10-5/2}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        // 检查核心内容，空格可能会被保留
        Assert.IsTrue(sql.Contains("[value] = [value]*2+10-5/2") || sql.Contains("[value]=[value]*2+10-5/2"), "应该保留所有运算符");
        
        Console.WriteLine($"运算符 SQL: {sql}");
    }

    [TestMethod]
    public void InlineExpression_WithComparison_ParsesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("status", "Status", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Status=CASE WHEN Status='pending' THEN 'active' ELSE Status END}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        Assert.IsTrue(sql.Contains("CASE WHEN [status]='pending' THEN 'active' ELSE [status] END"), "应该正确处理 CASE 表达式");
        
        Console.WriteLine($"CASE 表达式 SQL: {sql}");
    }

    #endregion

    #region 多方言一致性验证

    [TestMethod]
    public void InlineExpression_AllDialects_ConsistentBehavior()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("value", "Value", DbType.String, false),
        };

        var dialects = new[]
        {
            SqlDefine.SQLite,
            SqlDefine.PostgreSql,
            SqlDefine.MySql,
            SqlDefine.SqlServer,
            SqlDefine.Oracle,
            SqlDefine.DB2,
        };

        foreach (var dialect in dialects)
        {
            var context = new PlaceholderContext(dialect, "test", columns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Value=COALESCE(Value,'default')}} WHERE id = @id",
                context);

            var sql = template.Sql;
            
            // 验证基本结构
            Assert.IsTrue(sql.Contains("UPDATE"), $"{dialect} 应该包含 UPDATE");
            Assert.IsTrue(sql.Contains("SET"), $"{dialect} 应该包含 SET");
            Assert.IsTrue(sql.Contains("WHERE"), $"{dialect} 应该包含 WHERE");
            Assert.IsTrue(sql.Contains("COALESCE"), $"{dialect} 应该包含 COALESCE 函数");
            
            // 验证没有语法错误
            Assert.IsFalse(sql.Contains(",,"), $"{dialect} 不应该有连续的逗号");
            Assert.IsFalse(sql.Contains(", WHERE"), $"{dialect} SET 子句末尾不应该有逗号");
            
            Console.WriteLine($"{dialect}: {sql}");
        }
    }

    #endregion

    #region 实际问题场景

    [TestMethod]
    public void RealIssue_CoalesceWithComma_WorksCorrectly()
    {
        // 这是之前发现的 bug：COALESCE 函数中的逗号被错误地当作分隔符
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("status", "Status", DbType.String, true),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "orders", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Status=COALESCE(Status,'pending')}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        // 验证 COALESCE 函数完整，包含逗号
        Assert.AreEqual("UPDATE [orders] SET [status] = COALESCE([status],'pending') WHERE id = @id", sql);
        Assert.IsTrue(sql.Contains("COALESCE([status],'pending')"), "COALESCE 应该包含完整的参数");
        
        Console.WriteLine($"COALESCE 修复验证: {sql}");
    }

    [TestMethod]
    public void RealIssue_MultipleInlineExpressionsWithFunctions_WorksCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("email", "Email", DbType.String, false),
            new ColumnMeta("status", "Status", DbType.String, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Email=LOWER(TRIM(Email)),Status=COALESCE(Status,'active'),UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        // 验证所有表达式都正确
        Assert.IsTrue(sql.Contains("LOWER(TRIM([email]))"), "嵌套函数应该完整");
        Assert.IsTrue(sql.Contains("COALESCE([status],'active')"), "COALESCE 应该完整");
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"), "时间戳应该存在");
        
        // 验证表达式之间用逗号分隔（注意：函数内部的逗号不应该被计算）
        // 应该有3个表达式，所以有2个分隔逗号
        Assert.IsTrue(sql.Contains(", [status]") || sql.Contains(",[status]"), "应该有逗号分隔表达式");
        Assert.IsTrue(sql.Contains(", [updated_at]") || sql.Contains(",[updated_at]"), "应该有逗号分隔表达式");
        
        Console.WriteLine($"多表达式修复验证: {sql}");
    }

    [TestMethod]
    public void RealIssue_CaseExpressionWithMultipleWhen_WorksCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("priority", "Priority", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "tasks", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Priority=CASE WHEN Priority<1 THEN 1 WHEN Priority>10 THEN 10 ELSE Priority END}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        // 验证 CASE 表达式完整
        Assert.IsTrue(sql.Contains("CASE WHEN [priority]<1 THEN 1 WHEN [priority]>10 THEN 10 ELSE [priority] END"), 
            "CASE 表达式应该完整，包含所有 WHEN 子句");
        
        Console.WriteLine($"CASE 表达式验证: {sql}");
    }

    #endregion
}
