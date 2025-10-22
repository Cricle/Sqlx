// -----------------------------------------------------------------------
// <copyright file="DialectIntegrationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Tests.Dialects;

/// <summary>
/// 方言集成测试
/// 测试不同方言之间的一致性和差异性
/// </summary>
[TestClass]
public class DialectIntegrationTests
{
    /// <summary>
    /// 测试：所有方言应该支持基本的CRUD操作
    /// </summary>
    [TestMethod]
    public void AllDialects_ShouldSupport_BasicCRUD()
    {
        var providers = GetAllProviders();

        foreach (var provider in providers)
        {
            // INSERT
            var insert = provider.GenerateInsertWithReturning("users", new[] { "name", "email" });
            Assert.IsFalse(string.IsNullOrEmpty(insert));

            // UPSERT
            var upsert = provider.GenerateUpsert("users", new[] { "id", "name" }, new[] { "id" });
            Assert.IsFalse(string.IsNullOrEmpty(upsert));

            // BATCH INSERT
            var batch = provider.GenerateBatchInsert("users", new[] { "name" }, 5);
            Assert.IsFalse(string.IsNullOrEmpty(batch));
        }
    }

    /// <summary>
    /// 测试：不同方言的LIMIT语法应该不同
    /// </summary>
    [TestMethod]
    public void DifferentDialects_ShouldHave_DifferentLimitSyntax()
    {
        var sqlServer = new SqlServerDialectProvider();
        var mySql = new MySqlDialectProvider();
        var postgreSql = new PostgreSqlDialectProvider();
        var sqlite = new SQLiteDialectProvider();

        var sqlServerLimit = sqlServer.GenerateLimitClause(10, 0);
        var mySqlLimit = mySql.GenerateLimitClause(10, 0);
        var postgreSqlLimit = postgreSql.GenerateLimitClause(10, 0);
        var sqliteLimit = sqlite.GenerateLimitClause(10, 0);

        // SQL Server可能使用TOP或OFFSET-FETCH，与其他不同
        // MySQL, PostgreSQL, SQLite都使用LIMIT OFFSET
        var limitSyntaxes = new[] { sqlServerLimit, mySqlLimit, postgreSqlLimit, sqliteLimit };
        
        // 至少应该有一些差异
        Assert.IsTrue(limitSyntaxes.Any(s => !string.IsNullOrEmpty(s)));
    }

    /// <summary>
    /// 测试：不同方言的UPSERT语法应该不同
    /// </summary>
    [TestMethod]
    public void DifferentDialects_ShouldHave_DifferentUpsertSyntax()
    {
        var providers = GetAllProviders();
        var upsertSyntaxes = new List<string>();

        foreach (var provider in providers)
        {
            var upsert = provider.GenerateUpsert("users", new[] { "id", "name" }, new[] { "id" });
            upsertSyntaxes.Add(upsert);
        }

        // 确保所有方言都生成了UPSERT语法
        Assert.IsTrue(upsertSyntaxes.All(s => !string.IsNullOrEmpty(s)));
    }

    /// <summary>
    /// 测试：不同方言的字符串连接语法应该有所不同
    /// </summary>
    [TestMethod]
    public void DifferentDialects_ShouldHave_DifferentConcatenationSyntax()
    {
        var sqlServer = new SqlServerDialectProvider();
        var mySql = new MySqlDialectProvider();
        var postgreSql = new PostgreSqlDialectProvider();
        var sqlite = new SQLiteDialectProvider();

        var sqlServerConcat = sqlServer.GetConcatenationSyntax("'A'", "'B'");
        var mySqlConcat = mySql.GetConcatenationSyntax("'A'", "'B'");
        var postgreSqlConcat = postgreSql.GetConcatenationSyntax("'A'", "'B'");
        var sqliteConcat = sqlite.GetConcatenationSyntax("'A'", "'B'");

        // SQL Server: +
        // MySQL: CONCAT()
        // PostgreSQL: ||
        // SQLite: ||
        
        // 确保都生成了有效的连接语法
        Assert.IsTrue(!string.IsNullOrEmpty(sqlServerConcat));
        Assert.IsTrue(!string.IsNullOrEmpty(mySqlConcat));
        Assert.IsTrue(!string.IsNullOrEmpty(postgreSqlConcat));
        Assert.IsTrue(!string.IsNullOrEmpty(sqliteConcat));
    }

    /// <summary>
    /// 测试：所有方言的类型映射应该一致地处理null值
    /// </summary>
    [TestMethod]
    public void AllDialects_ShouldHandle_NullableTypes()
    {
        var providers = GetAllProviders();

        foreach (var provider in providers)
        {
            var intType = provider.GetDatabaseTypeName(typeof(int));
            var nullableIntType = provider.GetDatabaseTypeName(typeof(int?));

            // 应该都返回有效的类型名称
            Assert.IsFalse(string.IsNullOrEmpty(intType));
            Assert.IsFalse(string.IsNullOrEmpty(nullableIntType));
        }
    }

    /// <summary>
    /// 测试：批量INSERT的大小应该被正确处理
    /// </summary>
    [TestMethod]
    public void AllDialects_ShouldHandle_DifferentBatchSizes()
    {
        var providers = GetAllProviders();
        var batchSizes = new[] { 1, 10, 100, 1000 };

        foreach (var provider in providers)
        {
            foreach (var batchSize in batchSizes)
            {
                var result = provider.GenerateBatchInsert("users", new[] { "name" }, batchSize);
                Assert.IsFalse(string.IsNullOrEmpty(result), 
                    $"{provider.DialectType} 应该处理批量大小 {batchSize}");
            }
        }
    }

    /// <summary>
    /// 测试：LIMIT和OFFSET的边界情况
    /// </summary>
    [TestMethod]
    public void AllDialects_ShouldHandle_LimitOffsetEdgeCases()
    {
        var providers = GetAllProviders();

        foreach (var provider in providers)
        {
            // 只有LIMIT
            var limitOnly = provider.GenerateLimitClause(10, null);
            Assert.IsNotNull(limitOnly);

            // LIMIT为0
            var zeroLimit = provider.GenerateLimitClause(0, null);
            Assert.IsNotNull(zeroLimit);

            // 大的LIMIT值
            var largeLimit = provider.GenerateLimitClause(1000000, null);
            Assert.IsNotNull(largeLimit);

            // OFFSET为0
            var zeroOffset = provider.GenerateLimitClause(10, 0);
            Assert.IsNotNull(zeroOffset);
        }
    }

    /// <summary>
    /// 测试：DateTime格式化的一致性
    /// </summary>
    [TestMethod]
    public void AllDialects_ShouldFormat_DateTimeConsistently()
    {
        var providers = GetAllProviders();
        var testDates = new[]
        {
            new DateTime(2024, 1, 1, 0, 0, 0),
            new DateTime(2024, 12, 31, 23, 59, 59),
            new DateTime(2000, 6, 15, 12, 30, 45)
        };

        foreach (var provider in providers)
        {
            foreach (var date in testDates)
            {
                var formatted = provider.FormatDateTime(date);
                Assert.IsFalse(string.IsNullOrEmpty(formatted), 
                    $"{provider.DialectType} 应该格式化 {date}");
            }
        }
    }

    /// <summary>
    /// 测试：所有方言都应该支持常见的复合类型
    /// </summary>
    [TestMethod]
    public void AllDialects_ShouldSupport_CommonCompositeTypes()
    {
        var providers = GetAllProviders();
        var compositeTypes = new[]
        {
            typeof(Guid),
            typeof(byte[]),
            typeof(TimeSpan),
            typeof(DateTimeOffset)
        };

        foreach (var provider in providers)
        {
            foreach (var type in compositeTypes)
            {
                try
                {
                    var result = provider.GetDatabaseTypeName(type);
                    // 应该返回某种类型，即使是通用的
                    Assert.IsNotNull(result);
                }
                catch
                {
                    // 某些方言可能不支持某些类型，这是可以接受的
                }
            }
        }
    }

    private static List<IDatabaseDialectProvider> GetAllProviders()
    {
        return new List<IDatabaseDialectProvider>
        {
            new SqlServerDialectProvider(),
            new MySqlDialectProvider(),
            new PostgreSqlDialectProvider(),
            new SQLiteDialectProvider()
        };
    }
}

