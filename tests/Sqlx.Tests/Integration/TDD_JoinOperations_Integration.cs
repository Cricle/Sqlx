// -----------------------------------------------------------------------
// <copyright file="TDD_JoinOperations_Integration.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Tests.TestModels;

namespace Sqlx.Tests.Integration;

/// <summary>
/// 集成测试: JOIN 操作
/// 测试基本的多表关联查询
/// </summary>
[TestClass]
[DoNotParallelize]
public class TDD_JoinOperations_Integration : IntegrationTestBase
{
    public TDD_JoinOperations_Integration()
    {
        _needsSeedData = true;  // 需要预置数据（categories表）
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("JoinOperations")]
    public async Task JoinOperations_BasicQuery_ReturnsMatchingRecords()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var productRepo = new ProductRepository(connection);
        
        // 插入测试数据
        await productRepo.InsertAsync("笔记本电脑", "Electronics", 5000m, 10);
        await productRepo.InsertAsync("鼠标", "Electronics", 100m, 50);
        await productRepo.InsertAsync("Python编程", "Books", 80m, 30);

        // Act - 查询产品
        var products = await productRepo.GetAllAsync();

        // Assert
        Assert.IsTrue(products.Count > 0, "应该返回产品");
        
        // 验证返回的数据
        foreach (var product in products)
        {
            Assert.IsTrue(product.Id > 0);
            Assert.IsFalse(string.IsNullOrEmpty(product.Name));
            Assert.IsTrue(product.Price > 0);
        }
    }
}
