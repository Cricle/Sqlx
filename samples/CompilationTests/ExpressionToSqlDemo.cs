// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.CompilationTests;

using Sqlx;
using Sqlx.Annotations;
using System;
using System.Collections.Generic;
using System.Data.Common;
using static System.Console;

/// <summary>
/// 演示ExpressionToSql的各种用法，包括独立使用和作为参数传入.
/// </summary>
internal static class ExpressionToSqlDemo
{
    /// <summary>
    /// 演示ExpressionToSql的基本用法.
    /// </summary>
    public static void DemoBasicUsage()
    {
        WriteLine("=== ExpressionToSql基本用法演示 ===");

        // 1. 独立使用 - 直接生成SQL
        WriteLine("1. 独立生成SQL:");
        var query = Sqlx.Annotations.ExpressionToSql<PersonInformation>.Create()
            .Where(p => p.PersonId > 1)
            .Where(p => p.PersonName!.Contains("Alice"))
            .OrderBy(p => p.PersonName)
            .Take(10);

        var template = query.ToTemplate();
        WriteLine($"   SQL: {template.Sql}");
        WriteLine($"   参数: {string.Join(", ", template.Parameters)}");

        query.Dispose(); // 释放资源

        // 2. 复杂查询示例
        WriteLine("\n2. 复杂查询:");
        var complexQuery = Sqlx.Annotations.ExpressionToSql<PersonInformation>.Create()
            .Where(p => p.PersonId >= 1 && p.PersonId <= 10)
            .Where(p => p.PersonName!.StartsWith("A") || p.PersonName!.EndsWith("n"))
            .OrderBy(p => p.PersonId)
            .Skip(2)
            .Take(5);

        var complexTemplate = complexQuery.ToTemplate();
        WriteLine($"   SQL: {complexTemplate.Sql}");
        WriteLine($"   参数: {string.Join(", ", complexTemplate.Parameters)}");

        complexQuery.Dispose();

        // 3. 不同数据库方言
        WriteLine("\n3. 不同数据库方言:");
        DemoDialects();
    }

    /// <summary>
    /// 演示如何与Sqlx方法结合使用.
    /// </summary>
    public static void DemoWithSqlxIntegration(DbConnection connection)
    {
        WriteLine("\n=== ExpressionToSql与Sqlx集成演示 ===");

        try
        {
            var manager = new ExpressionDemoManager(connection);

            // 使用ExpressionToSql作为参数
            WriteLine("1. 作为方法参数使用:");
            var filterQuery = Sqlx.Annotations.ExpressionToSql<PersonInformation>.Create()
                .Where(p => p.PersonId <= 3);

            var results = manager.GetPersonsWithExpression(filterQuery);
            WriteLine($"   找到 {results.Count} 个结果");

            filterQuery.Dispose();

            // 使用扩展方法
            WriteLine("\n2. 使用扩展方法:");
            var extQuery = Sqlx.Annotations.ExpressionToSql<PersonInformation>.Create()
                .Where(p => p.PersonName!.Contains("o"));

            var extResults = connection.QueryWithExpression(extQuery);
            WriteLine($"   找到 {extResults.Count} 个结果");

            extQuery.Dispose();
        }
        catch (Exception ex)
        {
            WriteLine($"集成演示失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 演示不同数据库方言的SQL生成.
    /// </summary>
    private static void DemoDialects()
    {
        // MySQL
        var mysqlQuery = Sqlx.Annotations.ExpressionToSql<PersonInformation>.Create()
            .Where(p => p.PersonId > 5)
            .Take(10);
        WriteLine($"   MySQL: {mysqlQuery.ToSql()}");
        mysqlQuery.Dispose();

        // SQL Server
        var sqlServerQuery = Sqlx.Annotations.ExpressionToSql<PersonInformation>.Create()
            .Where(p => p.PersonId > 5)
            .Take(10);
        WriteLine($"   SQL Server: {sqlServerQuery.ToSql()}");
        sqlServerQuery.Dispose();

        // PostgreSQL
        var pgQuery = Sqlx.Annotations.ExpressionToSql<PersonInformation>.Create()
            .Where(p => p.PersonId > 5)
            .Take(10);
        WriteLine($"   PostgreSQL: {pgQuery.ToSql()}");
        pgQuery.Dispose();
    }
}

/// <summary>
/// 演示如何在类中使用ExpressionToSql.
/// </summary>
internal partial class ExpressionDemoManager
{
    private readonly DbConnection connection;

    public ExpressionDemoManager(DbConnection connection)
    {
        this.connection = connection;
    }

    /// <summary>
    /// 使用ExpressionToSql作为参数的方法 - SQL从表达式参数生成.
    /// </summary>
    public partial IList<PersonInformation> GetPersonsWithExpression([ExpressionToSql] ExpressionToSql<PersonInformation> query);

    /// <summary>
    /// 传统的RawSql方法，作为对比.
    /// </summary>
    [RawSql("SELECT person_id AS PersonId, person_name AS PersonName FROM person WHERE person_id > @minId")]
    public partial IList<PersonInformation> GetPersonsTraditional(int minId);
}

/// <summary>
/// ExpressionToSql的扩展方法演示.
/// </summary>
internal static partial class ExpressionExtensions
{
    /// <summary>
    /// 为DbConnection添加ExpressionToSql支持 - SQL从表达式参数生成.
    /// </summary>
    public static partial IList<PersonInformation> QueryWithExpression(
        this DbConnection connection,
        [ExpressionToSql] ExpressionToSql<PersonInformation> query);
}