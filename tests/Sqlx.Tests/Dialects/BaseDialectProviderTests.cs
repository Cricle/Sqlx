// -----------------------------------------------------------------------
// <copyright file="BaseDialectProviderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System;
using System.Linq;

namespace Sqlx.Tests.Dialects;

/// <summary>
/// BaseDialectProvider的单元测试
/// 测试基础方言提供器的通用功能
/// </summary>
[TestClass]
public class BaseDialectProviderTests
{
    /// <summary>
    /// 测试：所有方言提供器都应该继承自BaseDialectProvider
    /// </summary>
    [TestMethod]
    public void AllDialectProviders_ShouldInherit_FromBase()
    {
        var sqlServer = new SqlServerDialectProvider();
        var mySql = new MySqlDialectProvider();
        var postgreSql = new PostgreSqlDialectProvider();
        var sqlite = new SQLiteDialectProvider();

        Assert.IsInstanceOfType(sqlServer, typeof(IDatabaseDialectProvider));
        Assert.IsInstanceOfType(mySql, typeof(IDatabaseDialectProvider));
        Assert.IsInstanceOfType(postgreSql, typeof(IDatabaseDialectProvider));
        Assert.IsInstanceOfType(sqlite, typeof(IDatabaseDialectProvider));
    }

    /// <summary>
    /// 测试：所有方言提供器都应该有SqlDefine属性
    /// </summary>
    [TestMethod]
    public void AllDialectProviders_ShouldHave_SqlDefine()
    {
        var providers = new IDatabaseDialectProvider[]
        {
            new SqlServerDialectProvider(),
            new MySqlDialectProvider(),
            new PostgreSqlDialectProvider(),
            new SQLiteDialectProvider()
        };

        foreach (var provider in providers)
        {
            Assert.IsNotNull(provider.SqlDefine);
        }
    }

    /// <summary>
    /// 测试：所有方言提供器的DialectType应该唯一
    /// </summary>
    [TestMethod]
    public void AllDialectProviders_ShouldHave_UniqueDialectType()
    {
        var providers = new IDatabaseDialectProvider[]
        {
            new SqlServerDialectProvider(),
            new MySqlDialectProvider(),
            new PostgreSqlDialectProvider(),
            new SQLiteDialectProvider()
        };

        var dialectTypes = providers.Select(p => p.DialectType).ToList();
        var uniqueTypes = dialectTypes.Distinct().ToList();

        Assert.AreEqual(dialectTypes.Count, uniqueTypes.Count, "所有方言类型应该唯一");
    }

    /// <summary>
    /// 测试：所有方言提供器都应该能处理基本.NET类型
    /// </summary>
    [TestMethod]
    public void AllDialectProviders_ShouldHandle_CommonDotNetTypes()
    {
        var providers = new IDatabaseDialectProvider[]
        {
            new SqlServerDialectProvider(),
            new MySqlDialectProvider(),
            new PostgreSqlDialectProvider(),
            new SQLiteDialectProvider()
        };

        var commonTypes = new[]
        {
            typeof(int),
            typeof(string),
            typeof(DateTime),
            typeof(bool),
            typeof(decimal),
            typeof(long),
            typeof(double)
        };

        foreach (var provider in providers)
        {
            foreach (var type in commonTypes)
            {
                var result = provider.GetDatabaseTypeName(type);
                Assert.IsFalse(string.IsNullOrEmpty(result), 
                    $"{provider.DialectType} 应该为 {type.Name} 返回数据库类型");
            }
        }
    }

    /// <summary>
    /// 测试：所有方言提供器都应该能生成LIMIT子句
    /// </summary>
    [TestMethod]
    public void AllDialectProviders_ShouldGenerate_LimitClause()
    {
        var providers = new IDatabaseDialectProvider[]
        {
            new SqlServerDialectProvider(),
            new MySqlDialectProvider(),
            new PostgreSqlDialectProvider(),
            new SQLiteDialectProvider()
        };

        foreach (var provider in providers)
        {
            var result = provider.GenerateLimitClause(10, null);
            Assert.IsFalse(string.IsNullOrEmpty(result), 
                $"{provider.DialectType} 应该生成LIMIT子句");
        }
    }

    /// <summary>
    /// 测试：所有方言提供器都应该能生成当前日期时间语法
    /// </summary>
    [TestMethod]
    public void AllDialectProviders_ShouldGenerate_CurrentDateTimeSyntax()
    {
        var providers = new IDatabaseDialectProvider[]
        {
            new SqlServerDialectProvider(),
            new MySqlDialectProvider(),
            new PostgreSqlDialectProvider(),
            new SQLiteDialectProvider()
        };

        foreach (var provider in providers)
        {
            var result = provider.GetCurrentDateTimeSyntax();
            Assert.IsFalse(string.IsNullOrEmpty(result), 
                $"{provider.DialectType} 应该返回当前日期时间语法");
        }
    }

    /// <summary>
    /// 测试：所有方言提供器都应该能生成字符串连接语法
    /// </summary>
    [TestMethod]
    public void AllDialectProviders_ShouldGenerate_ConcatenationSyntax()
    {
        var providers = new IDatabaseDialectProvider[]
        {
            new SqlServerDialectProvider(),
            new MySqlDialectProvider(),
            new PostgreSqlDialectProvider(),
            new SQLiteDialectProvider()
        };

        foreach (var provider in providers)
        {
            var result = provider.GetConcatenationSyntax("'Hello'", "'World'");
            Assert.IsFalse(string.IsNullOrEmpty(result), 
                $"{provider.DialectType} 应该返回字符串连接语法");
        }
    }

    /// <summary>
    /// 测试：所有方言提供器都应该能格式化DateTime
    /// </summary>
    [TestMethod]
    public void AllDialectProviders_ShouldFormat_DateTime()
    {
        var providers = new IDatabaseDialectProvider[]
        {
            new SqlServerDialectProvider(),
            new MySqlDialectProvider(),
            new PostgreSqlDialectProvider(),
            new SQLiteDialectProvider()
        };

        var dateTime = new DateTime(2024, 1, 15, 10, 30, 45);

        foreach (var provider in providers)
        {
            var result = provider.FormatDateTime(dateTime);
            Assert.IsFalse(string.IsNullOrEmpty(result), 
                $"{provider.DialectType} 应该格式化DateTime");
        }
    }
}

