// -----------------------------------------------------------------------
// <copyright file="TDD_WindowFunctions_Integration.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Tests.TestModels;

namespace Sqlx.Tests.Integration;

/// <summary>
/// 集成测试: 窗口函数
/// 测试 {{row_number}} 等窗口函数占位符
/// </summary>
[TestClass]
public class TDD_WindowFunctions_Integration
{
    private static DatabaseFixture _fixture = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _fixture = new DatabaseFixture();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _fixture?.Dispose();
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("WindowFunctions")]
    public async Task WindowFunctions_RowNumber_PartitionsByCategory()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var productRepo = new ProductRepository(connection);
        
        // 创建不同分类的产品
        await productRepo.InsertAsync("笔记本A", "Electronics", 8000m, 10);
        await productRepo.InsertAsync("笔记本B", "Electronics", 6000m, 5);
        await productRepo.InsertAsync("笔记本C", "Electronics", 4000m, 8);
        await productRepo.InsertAsync("书籍A", "Books", 100m, 50);
        await productRepo.InsertAsync("书籍B", "Books", 80m, 30);
        await productRepo.InsertAsync("书籍C", "Books", 60m, 20);

        // Act - 使用 {{row_number}} 获取每个分类的前2名产品
        var topProducts = await advancedRepo.GetTopProductsByCategory(2);

        // Assert
        Assert.IsTrue(topProducts.Count >= 4, "应该至少有4个产品（每个分类2个）");
        
        // 验证 Electronics 分类
        var electronicsProducts = topProducts.Where(p => p.Category == "Electronics").ToList();
        Assert.AreEqual(2, electronicsProducts.Count, "Electronics 应该有2个产品");
        Assert.IsTrue(electronicsProducts.Any(p => p.Name == "笔记本A"), "应该包含最贵的笔记本A");
        Assert.IsTrue(electronicsProducts.Any(p => p.Name == "笔记本B"), "应该包含第二贵的笔记本B");
        
        // 验证 Books 分类
        var booksProducts = topProducts.Where(p => p.Category == "Books").ToList();
        Assert.AreEqual(2, booksProducts.Count, "Books 应该有2个产品");
        Assert.IsTrue(booksProducts.Any(p => p.Name == "书籍A"), "应该包含最贵的书籍A");
        Assert.IsTrue(booksProducts.Any(p => p.Name == "书籍B"), "应该包含第二贵的书籍B");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("WindowFunctions")]
    public async Task WindowFunctions_RowNumber_OrdersByPriceDesc()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var productRepo = new ProductRepository(connection);
        
        // 创建同一分类的产品
        await productRepo.InsertAsync("产品1", "Electronics", 1000m, 10);
        await productRepo.InsertAsync("产品2", "Electronics", 3000m, 5);
        await productRepo.InsertAsync("产品3", "Electronics", 2000m, 8);
        await productRepo.InsertAsync("产品4", "Electronics", 500m, 15);

        // Act - 获取前3名
        var topProducts = await advancedRepo.GetTopProductsByCategory(3);

        // Assert
        var electronicsProducts = topProducts.Where(p => p.Category == "Electronics")
            .OrderByDescending(p => p.Price)
            .ToList();
        
        Assert.AreEqual(3, electronicsProducts.Count);
        Assert.AreEqual("产品2", electronicsProducts[0].Name, "第1名应该是产品2 (3000)");
        Assert.AreEqual("产品3", electronicsProducts[1].Name, "第2名应该是产品3 (2000)");
        Assert.AreEqual("产品1", electronicsProducts[2].Name, "第3名应该是产品1 (1000)");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("WindowFunctions")]
    public async Task WindowFunctions_RowNumber_TopN_LimitsCorrectly()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var productRepo = new ProductRepository(connection);
        
        // 创建多个产品
        for (int i = 1; i <= 10; i++)
        {
            await productRepo.InsertAsync($"产品{i}", "Electronics", i * 100m, 10);
        }

        // Act - 只获取前1名
        var topProducts = await advancedRepo.GetTopProductsByCategory(1);

        // Assert
        var electronicsProducts = topProducts.Where(p => p.Category == "Electronics").ToList();
        Assert.AreEqual(1, electronicsProducts.Count, "应该只返回1个产品");
        Assert.AreEqual("产品10", electronicsProducts[0].Name, "应该是价格最高的产品10");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("WindowFunctions")]
    public async Task WindowFunctions_RowNumber_MultipleCategories_PartitionsIndependently()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.CleanupData(SqlDefineTypes.SQLite);
        var advancedRepo = new AdvancedRepository(connection);
        var productRepo = new ProductRepository(connection);
        
        // 创建3个分类的产品
        await productRepo.InsertAsync("电子1", "Electronics", 5000m, 10);
        await productRepo.InsertAsync("电子2", "Electronics", 3000m, 5);
        await productRepo.InsertAsync("书籍1", "Books", 100m, 50);
        await productRepo.InsertAsync("书籍2", "Books", 80m, 30);
        await productRepo.InsertAsync("服装1", "Clothing", 200m, 20);
        await productRepo.InsertAsync("服装2", "Clothing", 150m, 15);

        // Act - 每个分类获取前1名
        var topProducts = await advancedRepo.GetTopProductsByCategory(1);

        // Assert
        Assert.AreEqual(3, topProducts.Count, "应该有3个分类各1个产品");
        
        Assert.IsTrue(topProducts.Any(p => p.Name == "电子1"), "应该包含 Electronics 的第1名");
        Assert.IsTrue(topProducts.Any(p => p.Name == "书籍1"), "应该包含 Books 的第1名");
        Assert.IsTrue(topProducts.Any(p => p.Name == "服装1"), "应该包含 Clothing 的第1名");
    }
}
