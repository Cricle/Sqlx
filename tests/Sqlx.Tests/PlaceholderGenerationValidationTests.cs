// <copyright file="PlaceholderGenerationValidationTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// 重点验证占位符生成的正确性，确保没有语法错误和逻辑问题.
/// </summary>
[TestClass]
public class PlaceholderGenerationValidationTests
{
    private static readonly ColumnMeta[] TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, false),
        new ColumnMeta("age", "Age", DbType.Int32, false),
        new ColumnMeta("status", "Status", DbType.String, false),
        new ColumnMeta("version", "Version", DbType.Int32, false),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
    };

    #region SET 占位符验证

    [TestMethod]
    public void Set_BasicGeneration_NoSyntaxErrors()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);

        var sql = template.Sql;
        
        // 验证基本结构
        Assert.IsTrue(sql.StartsWith("UPDATE [users] SET"), "应该以 UPDATE [users] SET 开头");
        Assert.IsTrue(sql.Contains("WHERE id = @id"), "应该包含 WHERE 子句");
        
        // 验证没有多余的逗号
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        Assert.IsFalse(sql.Contains(", WHERE"), "SET 子句末尾不应该有逗号");
        Assert.IsFalse(sql.Contains("SET ,"), "SET 后面不应该直接跟逗号");
        
        // 验证所有列都有对应的参数
        Assert.IsTrue(sql.Contains("[name] = @name"), "应该包含 name 列");
        Assert.IsTrue(sql.Contains("[email] = @email"), "应该包含 email 列");
        Assert.IsTrue(sql.Contains("[age] = @age"), "应该包含 age 列");
        
        // 验证排除的列不存在
        Assert.IsFalse(sql.Contains("[id] = @id,") || sql.Contains(", [id] = @id"), "不应该在 SET 子句中包含 id 列");
    }

    [TestMethod]
    public void Set_WithInlineExpression_NoSyntaxErrors()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id,CreatedAt --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        // 验证基本结构
        Assert.IsTrue(sql.StartsWith("UPDATE [users] SET"), "应该以 UPDATE 开头");
        Assert.IsTrue(sql.EndsWith("WHERE id = @id"), "应该以 WHERE 结尾");
        
        // 验证没有语法错误
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        Assert.IsFalse(sql.Contains(", WHERE"), "SET 子句末尾不应该有逗号");
        
        // 验证内联表达式正确生成
        Assert.IsTrue(sql.Contains("[version] = [version]+1"), "应该包含版本递增表达式");
        Assert.IsTrue(sql.Contains("[updated_at] = CURRENT_TIMESTAMP"), "应该包含时间戳表达式");
        
        // 验证常规列仍然使用参数
        Assert.IsTrue(sql.Contains("[name] = @name"), "常规列应该使用参数");
        Assert.IsTrue(sql.Contains("[email] = @email"), "常规列应该使用参数");
        
        // 验证排除的列不存在
        Assert.IsFalse(sql.Contains("[id] ="), "不应该包含 id 列");
        Assert.IsFalse(sql.Contains("[created_at] ="), "不应该包含 created_at 列");
    }

    [TestMethod]
    public void Set_ComplexInlineExpression_WithParenthesesAndCommas_NoSyntaxErrors()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("status", "Status", DbType.String, false),
            new ColumnMeta("priority", "Priority", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "tasks", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Status=COALESCE(Status,'pending'),Priority=CASE WHEN Priority>5 THEN 5 ELSE Priority END}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        // 验证 COALESCE 表达式完整
        Assert.IsTrue(sql.Contains("COALESCE([status],'pending')"), "COALESCE 表达式应该完整，包含逗号");
        
        // 验证 CASE 表达式完整
        Assert.IsTrue(sql.Contains("CASE WHEN"), "应该包含 CASE WHEN");
        Assert.IsTrue(sql.Contains("THEN"), "应该包含 THEN");
        Assert.IsTrue(sql.Contains("ELSE"), "应该包含 ELSE");
        Assert.IsTrue(sql.Contains("END"), "应该包含 END");
        
        // 验证没有语法错误
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        Assert.IsFalse(sql.Contains(", WHERE"), "SET 子句末尾不应该有逗号");
    }

    [TestMethod]
    public void Set_AllDialects_GenerateValidSQL()
    {
        var dialects = new[]
        {
            (SqlDefine.SQLite, "[", "]", "@"),
            (SqlDefine.PostgreSql, "\"", "\"", "$"),
            (SqlDefine.MySql, "`", "`", "@"),
            (SqlDefine.SqlServer, "[", "]", "@"),
            (SqlDefine.Oracle, "\"", "\"", ":"),
        };

        foreach (var (dialect, leftQuote, rightQuote, paramPrefix) in dialects)
        {
            var context = new PlaceholderContext(dialect, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id,CreatedAt --inline Version=Version+1}} WHERE id = @id",
                context);

            var sql = template.Sql;
            
            // 验证列名被正确包装
            Assert.IsTrue(sql.Contains($"{leftQuote}name{rightQuote}"), $"{dialect} 应该正确包装列名");
            Assert.IsTrue(sql.Contains($"{leftQuote}version{rightQuote}"), $"{dialect} 应该正确包装列名");
            
            // 验证参数前缀正确（注意：模板中的 @id 不会被转换）
            Assert.IsTrue(sql.Contains($"{leftQuote}name{rightQuote} = {paramPrefix}name"), $"{dialect} 应该使用正确的参数前缀");
            
            // 验证没有语法错误
            Assert.IsFalse(sql.Contains(",,"), $"{dialect} 不应该有连续的逗号");
            Assert.IsFalse(sql.Contains(", WHERE"), $"{dialect} SET 子句末尾不应该有逗号");
        }
    }

    #endregion

    #region VALUES 占位符验证

    [TestMethod]
    public void Values_BasicGeneration_NoSyntaxErrors()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", context);

        var sql = template.Sql;
        
        // 验证基本结构
        Assert.IsTrue(sql.StartsWith("INSERT INTO [users]"), "应该以 INSERT INTO 开头");
        Assert.IsTrue(sql.Contains("VALUES"), "应该包含 VALUES");
        
        // 验证括号匹配
        var openParens = 0;
        var closeParens = 0;
        foreach (var ch in sql)
        {
            if (ch == '(') openParens++;
            if (ch == ')') closeParens++;
        }
        Assert.AreEqual(openParens, closeParens, "括号应该匹配");
        
        // 验证没有多余的逗号
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        Assert.IsFalse(sql.Contains("(,"), "左括号后不应该有逗号");
        Assert.IsFalse(sql.Contains(",)"), "右括号前不应该有逗号");
        
        // 验证列数和值数匹配
        var columnsSection = sql.Substring(sql.IndexOf('('), sql.IndexOf(") VALUES") - sql.IndexOf('('));
        var valuesSection = sql.Substring(sql.LastIndexOf('('));
        
        var columnCount = columnsSection.Split(',').Length;
        var valueCount = valuesSection.Split(',').Length;
        Assert.AreEqual(columnCount, valueCount, "列数和值数应该匹配");
    }

    [TestMethod]
    public void Values_WithInlineExpression_NoSyntaxErrors()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP,Version=1}})",
            context);

        var sql = template.Sql;
        
        // 验证基本结构
        Assert.IsTrue(sql.Contains("INSERT INTO"), "应该包含 INSERT INTO");
        Assert.IsTrue(sql.Contains("VALUES"), "应该包含 VALUES");
        
        // 验证内联表达式
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"), "应该包含 CURRENT_TIMESTAMP");
        Assert.IsTrue(sql.Contains("1"), "应该包含版本号 1");
        
        // 验证没有语法错误
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        Assert.IsFalse(sql.Contains("(,"), "左括号后不应该有逗号");
        Assert.IsFalse(sql.Contains(",)"), "右括号前不应该有逗号");
    }

    [TestMethod]
    public void Values_ComplexInlineExpression_WithNestedFunctions_NoSyntaxErrors()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("email", "Email", DbType.String, false),
            new ColumnMeta("status", "Status", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Email=LOWER(TRIM(Email)),Status=COALESCE(Status,'active'),CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        
        // 验证嵌套函数完整
        Assert.IsTrue(sql.Contains("LOWER(TRIM([email]))"), "嵌套函数应该完整");
        Assert.IsTrue(sql.Contains("COALESCE([status],'active')"), "COALESCE 应该完整，包含逗号");
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"), "时间戳函数应该存在");
        
        // 验证没有语法错误
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        Assert.IsFalse(sql.Contains("(,"), "左括号后不应该有逗号");
        Assert.IsFalse(sql.Contains(",)"), "右括号前不应该有逗号");
    }

    #endregion

    #region 组合场景验证

    [TestMethod]
    public void CompleteInsertStatement_AllPlaceholders_NoSyntaxErrors()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id,CreatedAt,UpdatedAt}}) VALUES ({{values --exclude Id,CreatedAt,UpdatedAt --inline CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP,Version=1}})",
            context);

        var sql = template.Sql;
        
        // 验证完整的 INSERT 语句结构
        Assert.IsTrue(sql.StartsWith("INSERT INTO [users]"), "应该以 INSERT INTO 开头");
        Assert.IsTrue(sql.Contains(") VALUES ("), "应该包含 VALUES 子句");
        Assert.IsTrue(sql.EndsWith(")"), "应该以右括号结尾");
        
        // 验证没有语法错误
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        Assert.IsFalse(sql.Contains("(,"), "左括号后不应该有逗号");
        Assert.IsFalse(sql.Contains(",)"), "右括号前不应该有逗号");
        
        Console.WriteLine($"生成的 SQL: {sql}");
    }

    [TestMethod]
    public void CompleteUpdateStatement_AllPlaceholders_NoSyntaxErrors()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id,CreatedAt --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id AND version = @version",
            context);

        var sql = template.Sql;
        
        // 验证完整的 UPDATE 语句结构
        Assert.IsTrue(sql.StartsWith("UPDATE [users] SET"), "应该以 UPDATE SET 开头");
        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
        
        // 验证没有语法错误
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        Assert.IsFalse(sql.Contains(", WHERE"), "SET 子句末尾不应该有逗号");
        Assert.IsFalse(sql.Contains("SET ,"), "SET 后面不应该直接跟逗号");
        
        Console.WriteLine($"生成的 SQL: {sql}");
    }

    [TestMethod]
    public void CompleteSelectStatement_AllPlaceholders_NoSyntaxErrors()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns --exclude CreatedAt,UpdatedAt}} FROM {{table}} WHERE status = @status ORDER BY name",
            context);

        var sql = template.Sql;
        
        // 验证完整的 SELECT 语句结构
        Assert.IsTrue(sql.StartsWith("SELECT"), "应该以 SELECT 开头");
        Assert.IsTrue(sql.Contains("FROM [users]"), "应该包含 FROM 子句");
        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE 子句");
        Assert.IsTrue(sql.Contains("ORDER BY"), "应该包含 ORDER BY 子句");
        
        // 验证没有语法错误
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        Assert.IsFalse(sql.Contains(", FROM"), "列列表末尾不应该有逗号");
        Assert.IsFalse(sql.Contains("SELECT ,"), "SELECT 后面不应该直接跟逗号");
        
        Console.WriteLine($"生成的 SQL: {sql}");
    }

    #endregion

    #region 边界情况验证

    [TestMethod]
    public void Set_OnlyOneColumn_NoTrailingComma()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);

        var sql = template.Sql;
        
        Assert.AreEqual("UPDATE [users] SET [name] = @name WHERE id = @id", sql, "单列 SET 应该没有逗号");
        Assert.IsFalse(sql.Contains(","), "单列不应该有逗号");
    }

    [TestMethod]
    public void Values_OnlyOneColumn_NoTrailingComma()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", context);

        var sql = template.Sql;
        
        Assert.AreEqual("INSERT INTO [users] ([name]) VALUES (@name)", sql, "单列 VALUES 应该没有逗号");
        
        // 验证括号内没有逗号
        var valuesSection = sql.Substring(sql.LastIndexOf('('));
        Assert.IsFalse(valuesSection.Contains(","), "单个值不应该有逗号");
    }

    [TestMethod]
    public void Set_EmptyAfterExclude_ReturnsEmpty()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id,Name}} WHERE id = @id", context);

        var sql = template.Sql;
        
        Assert.AreEqual("UPDATE [users] SET  WHERE id = @id", sql, "排除所有列后应该返回空");
    }

    [TestMethod]
    public void InlineExpression_WithQuotedString_PreservesQuotes()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("status", "Status", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "tasks", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Status='completed'}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        Assert.IsTrue(sql.Contains("[status] = 'completed'"), "应该保留字符串引号");
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
    }

    [TestMethod]
    public void InlineExpression_WithMultipleNestedParentheses_HandlesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("result", "Result", DbType.Decimal, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "calculations", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Result=((Result+@a)*@b)/(@c+1)}} WHERE id = @id",
            context);

        var sql = template.Sql;
        
        Assert.IsTrue(sql.Contains("(([result]+@a)*@b)/(@c+1)"), "应该保留所有括号");
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
    }

    #endregion

    #region 实际使用场景验证

    [TestMethod]
    public void RealWorld_UserRegistration_GeneratesValidSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("username", "Username", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, false),
            new ColumnMeta("password_hash", "PasswordHash", DbType.String, false),
            new ColumnMeta("status", "Status", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Status='active',CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        
        // 验证完整性
        Assert.IsTrue(sql.Contains("INSERT INTO"), "应该是 INSERT 语句");
        Assert.IsTrue(sql.Contains("VALUES"), "应该包含 VALUES");
        Assert.IsTrue(sql.Contains("@username"), "应该包含用户名参数");
        Assert.IsTrue(sql.Contains("@email"), "应该包含邮箱参数");
        Assert.IsTrue(sql.Contains("@password_hash"), "应该包含密码参数");
        Assert.IsTrue(sql.Contains("'active'"), "应该包含默认状态");
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"), "应该包含时间戳");
        
        // 验证没有语法错误
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        Assert.IsFalse(sql.Contains("(,"), "左括号后不应该有逗号");
        Assert.IsFalse(sql.Contains(",)"), "右括号前不应该有逗号");
        
        Console.WriteLine($"用户注册 SQL: {sql}");
    }

    [TestMethod]
    public void RealWorld_OrderUpdate_GeneratesValidSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("status", "Status", DbType.String, false),
            new ColumnMeta("total_amount", "TotalAmount", DbType.Decimal, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "orders", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id AND version = @version",
            context);

        var sql = template.Sql;
        
        // 验证完整性
        Assert.IsTrue(sql.Contains("UPDATE"), "应该是 UPDATE 语句");
        Assert.IsTrue(sql.Contains("SET"), "应该包含 SET");
        Assert.IsTrue(sql.Contains("WHERE"), "应该包含 WHERE");
        Assert.IsTrue(sql.Contains("[status] = @status"), "应该包含状态参数");
        Assert.IsTrue(sql.Contains("[total_amount] = @total_amount"), "应该包含金额参数");
        Assert.IsTrue(sql.Contains("[version] = [version]+1"), "应该包含版本递增");
        Assert.IsTrue(sql.Contains("[updated_at] = CURRENT_TIMESTAMP"), "应该包含时间戳");
        Assert.IsTrue(sql.Contains("version = @version"), "WHERE 子句应该包含版本检查");
        
        // 验证没有语法错误
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        Assert.IsFalse(sql.Contains(", WHERE"), "SET 子句末尾不应该有逗号");
        
        Console.WriteLine($"订单更新 SQL: {sql}");
    }

    [TestMethod]
    public void RealWorld_InventoryDeduction_GeneratesValidSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("product_id", "ProductId", DbType.Int64, false),
            new ColumnMeta("quantity", "Quantity", DbType.Int32, false),
            new ColumnMeta("reserved", "Reserved", DbType.Int32, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "inventory", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id,ProductId --inline Quantity=Quantity-@deduct,Reserved=Reserved+@deduct,UpdatedAt=CURRENT_TIMESTAMP}} WHERE product_id = @productId AND quantity >= @deduct",
            context);

        var sql = template.Sql;
        
        // 验证完整性
        Assert.IsTrue(sql.Contains("[quantity] = [quantity]-@deduct"), "应该包含库存扣减");
        Assert.IsTrue(sql.Contains("[reserved] = [reserved]+@deduct"), "应该包含预留增加");
        Assert.IsTrue(sql.Contains("[updated_at] = CURRENT_TIMESTAMP"), "应该包含时间戳");
        Assert.IsTrue(sql.Contains("quantity >= @deduct"), "WHERE 子句应该检查库存充足");
        
        // 验证没有语法错误
        Assert.IsFalse(sql.Contains(",,"), "不应该有连续的逗号");
        Assert.IsFalse(sql.Contains(", WHERE"), "SET 子句末尾不应该有逗号");
        
        Console.WriteLine($"库存扣减 SQL: {sql}");
    }

    #endregion
}
