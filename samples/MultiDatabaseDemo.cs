// -----------------------------------------------------------------------
// <copyright file="MultiDatabaseDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Sqlx.Generator.Core;

namespace Sqlx.Samples;

/// <summary>
/// 多数据库模板引擎演示 - 展示"写一次，处处运行"的强大功能
/// </summary>
public class MultiDatabaseDemo
{
    private readonly SqlTemplateEngine _engine = new();

    /// <summary>
    /// 演示同一个模板在不同数据库中的表现
    /// </summary>
    public void DemonstrateWriteOnceRunEverywhere()
    {
        Console.WriteLine("🌟 === Sqlx多数据库模板引擎演示 ===");
        Console.WriteLine("展示同一个模板在不同数据库中的自动适配\n");

        // 定义一个通用模板
        var template = "SELECT {{columns:auto}} FROM {{table:quoted}} WHERE {{where:id}} {{orderby:name}} {{limit:auto|default=10}}";

        Console.WriteLine($"📝 原始模板:\n   {template}\n");

        // 测试所有支持的数据库
        DemonstrateDialect("SQL Server", SqlDefine.SqlServer, template);
        DemonstrateDialect("MySQL", SqlDefine.MySql, template);
        DemonstrateDialect("PostgreSQL", SqlDefine.PostgreSql, template);
        DemonstrateDialect("SQLite", SqlDefine.SQLite, template);
        DemonstrateDialect("Oracle", SqlDefine.Oracle, template);
        DemonstrateDialect("DB2", SqlDefine.DB2, template);
    }

    /// <summary>
    /// 演示复杂查询的多数据库支持
    /// </summary>
    public void DemonstrateComplexQueries()
    {
        Console.WriteLine("\n🚀 === 复杂查询多数据库支持演示 ===\n");

        var complexTemplate = "{{select:distinct}} {{columns:auto|exclude=Password}} FROM {{table:quoted}} " +
                            "{{join:inner|table=Department|on=u.DeptId = d.Id}} " +
                            "WHERE {{where:auto}} {{groupby:department}} " +
                            "{{orderby:salary|desc}} {{limit:mysql|default=20}}";

        Console.WriteLine($"📋 复杂模板:\n   {complexTemplate}\n");

        DemonstrateDialect("SQL Server", SqlDefine.SqlServer, complexTemplate);
        DemonstrateDialect("MySQL", SqlDefine.MySql, complexTemplate);
        DemonstrateDialect("PostgreSQL", SqlDefine.PostgreSql, complexTemplate);
    }

    /// <summary>
    /// 演示安全特性
    /// </summary>
    public void DemonstrateSecurity()
    {
        Console.WriteLine("\n🛡️ === 安全特性演示 ===\n");

        // SQL注入检测
        var dangerousTemplate = "SELECT * FROM users WHERE name = 'test'; DROP TABLE users; --";
        Console.WriteLine("🚨 SQL注入检测:");
        Console.WriteLine($"   模板: {dangerousTemplate}");

        var result = _engine.ProcessTemplate(dangerousTemplate, null!, null, "users", SqlDefine.SqlServer);
        Console.WriteLine($"   结果: {(result.Errors.Count > 0 ? "❌ 检测到SQL注入" : "✅ 安全")}");

        if (result.Errors.Count > 0)
        {
            Console.WriteLine($"   错误: {result.Errors[0]}\n");
        }

        // MySQL特定安全检查
        var mysqlDangerous = "SELECT * FROM users INTO OUTFILE '/tmp/users.txt'";
        Console.WriteLine("🚨 MySQL文件操作检测:");
        Console.WriteLine($"   模板: {mysqlDangerous}");

        result = _engine.ProcessTemplate(mysqlDangerous, null!, null, "users", SqlDefine.MySql);
        Console.WriteLine($"   结果: {(result.Errors.Count > 0 ? "❌ 检测到危险操作" : "✅ 安全")}");

        if (result.Errors.Count > 0)
        {
            Console.WriteLine($"   错误: {result.Errors[0]}\n");
        }
    }

    /// <summary>
    /// 演示数据库特定功能
    /// </summary>
    public void DemonstrateDatabaseSpecificFeatures()
    {
        Console.WriteLine("\n⚡ === 数据库特定功能演示 ===\n");

        // 分页语法差异
        Console.WriteLine("📄 分页语法差异:");
        ShowPaginationSyntax("SQL Server", SqlDefine.SqlServer);
        ShowPaginationSyntax("MySQL", SqlDefine.MySql);
        ShowPaginationSyntax("PostgreSQL", SqlDefine.PostgreSql);
        ShowPaginationSyntax("Oracle", SqlDefine.Oracle);

        Console.WriteLine();

        // 日期函数差异
        Console.WriteLine("📅 日期函数差异:");
        ShowDateFunction("SQL Server", SqlDefine.SqlServer);
        ShowDateFunction("MySQL", SqlDefine.MySql);
        ShowDateFunction("PostgreSQL", SqlDefine.PostgreSql);
        ShowDateFunction("SQLite", SqlDefine.SQLite);
        ShowDateFunction("Oracle", SqlDefine.Oracle);
    }

    private void DemonstrateDialect(string dialectName, SqlDefine dialect, string template)
    {
        Console.WriteLine($"🗄️ {dialectName}:");
        Console.WriteLine($"   列引用: {dialect.ColumnLeft}column{dialect.ColumnRight}");
        Console.WriteLine($"   参数前缀: {dialect.ParameterPrefix}");

        // 模拟处理结果（简化演示）
        var processedSql = ProcessTemplateForDemo(template, dialect);
        Console.WriteLine($"   生成SQL: {processedSql}\n");
    }

    private void ShowPaginationSyntax(string dialectName, SqlDefine dialect)
    {
        var paginationSql = dialect switch
        {
            var d when d.Equals(SqlDefine.SqlServer) => "SELECT TOP 10 * FROM users ORDER BY id",
            var d when d.Equals(SqlDefine.MySql) => "SELECT * FROM users ORDER BY id LIMIT 10",
            var d when d.Equals(SqlDefine.PostgreSql) => "SELECT * FROM users ORDER BY id LIMIT 10",
            var d when d.Equals(SqlDefine.Oracle) => "SELECT * FROM users WHERE ROWNUM <= 10 ORDER BY id",
            _ => "SELECT * FROM users ORDER BY id LIMIT 10"
        };

        Console.WriteLine($"   {dialectName}: {paginationSql}");
    }

    private void ShowDateFunction(string dialectName, SqlDefine dialect)
    {
        var dateFunction = SqlTemplateEngineExtensions.DatabaseSpecificFeatures.GetCurrentDateFunction(dialect);
        Console.WriteLine($"   {dialectName}: {dateFunction}");
    }

    private string ProcessTemplateForDemo(string template, SqlDefine dialect)
    {
        // 简化的模板处理演示
        return template
            .Replace("{{columns:auto}}", $"{dialect.WrapColumn("Id")}, {dialect.WrapColumn("Name")}, {dialect.WrapColumn("Email")}")
            .Replace("{{table:quoted}}", dialect.WrapColumn("User"))
            .Replace("{{where:id}}", $"{dialect.WrapColumn("Id")} = {dialect.ParameterPrefix}id")
            .Replace("{{orderby:name}}", $"ORDER BY {dialect.WrapColumn("Name")} ASC")
            .Replace("{{limit:auto|default=10}}", GetLimitClause(dialect, 10))
            .Replace("{{select:distinct}}", "SELECT DISTINCT")
            .Replace("{{columns:auto|exclude=Password}}", $"{dialect.WrapColumn("Id")}, {dialect.WrapColumn("Name")}, {dialect.WrapColumn("Email")}")
            .Replace("{{join:inner|table=Department|on=u.DeptId = d.Id}}", $"INNER JOIN {dialect.WrapColumn("Department")} d ON u.DeptId = d.Id")
            .Replace("{{where:auto}}", $"{dialect.WrapColumn("Name")} = {dialect.ParameterPrefix}name AND {dialect.WrapColumn("Age")} >= {dialect.ParameterPrefix}age")
            .Replace("{{groupby:department}}", $"GROUP BY {dialect.WrapColumn("Department")}")
            .Replace("{{orderby:salary|desc}}", $"ORDER BY {dialect.WrapColumn("Salary")} DESC")
            .Replace("{{limit:mysql|default=20}}", GetLimitClause(dialect, 20));
    }

    private string GetLimitClause(SqlDefine dialect, int count)
    {
        return dialect switch
        {
            var d when d.Equals(SqlDefine.SqlServer) => $"TOP {count}",
            var d when d.Equals(SqlDefine.Oracle) => $"ROWNUM <= {count}",
            _ => $"LIMIT {count}"
        };
    }

    /// <summary>
    /// 运行完整演示
    /// </summary>
    public static void RunDemo()
    {
        var demo = new MultiDatabaseDemo();

        try
        {
            demo.DemonstrateWriteOnceRunEverywhere();
            demo.DemonstrateComplexQueries();
            demo.DemonstrateSecurity();
            demo.DemonstrateDatabaseSpecificFeatures();

            Console.WriteLine("✅ === 演示完成 ===");
            Console.WriteLine("🎉 Sqlx多数据库模板引擎成功展示了"写一次，处处运行"的强大功能！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 演示过程中发生错误: {ex.Message}");
        }
    }
}

