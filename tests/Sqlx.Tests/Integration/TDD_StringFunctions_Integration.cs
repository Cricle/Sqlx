// -----------------------------------------------------------------------
// <copyright file="TDD_StringFunctions_Integration.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Tests.TestModels;

namespace Sqlx.Tests.Integration;

/// <summary>
/// 字符串函数占位符集成测试
/// 测试: {{like}}, {{in}}, {{between}}, {{distinct}}, {{coalesce}}
/// </summary>
[TestClass]
[DoNotParallelize]
public class TDD_StringFunctions_Integration : IntegrationTestBase
{
    public TDD_StringFunctions_Integration()
    {
        _needsSeedData = false;  // 每个测试自己插入数据
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("StringFunctions")]
    public async Task StringFunctions_Like_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var productRepo = new ProductRepository(connection);
        var products = IntegrationTestHelpers.GenerateTestProducts(3);
        foreach (var product in products)
        {
            await productRepo.InsertAsync(product.Name, product.Category, product.Price, product.Stock);
        }

        // Act - 使用 {{like @pattern}} 占位符
        var searchResults = await productRepo.SearchByNameAsync("%Mac%");

        // Assert
        Assert.AreEqual(1, searchResults.Count, "应该找到1个包含'Mac'的产品");
        Assert.AreEqual("MacBook Pro", searchResults[0].Name);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("StringFunctions")]
    public async Task StringFunctions_In_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var productRepo = new ProductRepository(connection);
        var products = IntegrationTestHelpers.GenerateTestProducts(3);
        var insertedIds = new List<long>();
        foreach (var product in products)
        {
            var id = await productRepo.InsertAsync(product.Name, product.Category, product.Price, product.Stock);
            insertedIds.Add(id);
        }

        // Act - 使用 {{in @ids}} 占位符，使用实际插入的 ID
        var ids = insertedIds.Take(2).ToArray();
        var results = await productRepo.GetByIdsAsync(ids);

        // Assert
        Assert.AreEqual(2, results.Count, "应该找到2个产品");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("StringFunctions")]
    public async Task StringFunctions_Between_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var productRepo = new ProductRepository(connection);
        var products = IntegrationTestHelpers.GenerateTestProducts(3);
        foreach (var product in products)
        {
            await productRepo.InsertAsync(product.Name, product.Category, product.Price, product.Stock);
        }

        // Act - 使用 {{between @minPrice, @maxPrice}} 占位符
        var results = await productRepo.GetByPriceRangeAsync(50m, 1000m);

        // Assert
        Assert.AreEqual(2, results.Count, "应该找到2个价格在50-1000之间的产品");
        Assert.IsTrue(results.All(p => p.Price >= 50m && p.Price <= 1000m));
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("StringFunctions")]
    public async Task StringFunctions_Coalesce_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var userRepo = new UserRepository(connection);
        var userId = await userRepo.InsertAsync("测试用户", "test@example.com", 25, 5000m, DateTime.Now, true);

        // Act - 使用 {{coalesce email, 'default'}} 占位符
        var user = await userRepo.GetUserWithDefaultEmailAsync(userId);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("test@example.com", user.Email, "应该返回实际的email，不是默认值");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("StringFunctions")]
    public async Task StringFunctions_Distinct_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var userRepo = new UserRepository(connection);
        await userRepo.InsertAsync("用户1", "user1@example.com", 25, 1000m, DateTime.Now, true);
        await userRepo.InsertAsync("用户2", "user2@example.com", 25, 2000m, DateTime.Now, true);
        await userRepo.InsertAsync("用户3", "user3@example.com", 30, 3000m, DateTime.Now, true);

        // Act - 使用 {{distinct age}} 占位符
        var distinctAges = await userRepo.GetDistinctAgesAsync();

        // Debug output
        Console.WriteLine($"Distinct ages count: {distinctAges.Count}");
        Console.WriteLine($"Distinct ages: {string.Join(", ", distinctAges)}");

        // Assert
        Assert.AreEqual(2, distinctAges.Count, $"应该有2个不同的年龄，但实际得到{distinctAges.Count}个: {string.Join(", ", distinctAges)}");
        Assert.IsTrue(distinctAges.Contains(25));
        Assert.IsTrue(distinctAges.Contains(30));
    }
}
