// -----------------------------------------------------------------------
// <copyright file="ECommerceScenarios_SqlValidation.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.E2E.Scenarios;

/// <summary>
/// TDD tests to validate SQL generation for E-commerce scenarios.
/// Verifies that placeholders are correctly replaced and SQL is valid for each dialect.
/// </summary>
[TestClass]
public class ECommerceScenarios_SqlValidation
{
    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void EComProductRepository_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var repoType = typeof(EComProductRepository);
        
        // Act
        var tableNameAttr = repoType.GetCustomAttribute<TableNameAttribute>();
        var sqlDefineAttr = repoType.GetCustomAttribute<SqlDefineAttribute>();
        
        // Assert
        Assert.IsNotNull(tableNameAttr, "Repository should have TableName attribute");
        Assert.AreEqual("ecom_products", tableNameAttr.TableName, "Table name should be 'ecom_products'");
        Assert.IsNotNull(sqlDefineAttr, "Repository should have SqlDefine attribute");
        Assert.AreEqual(SqlDefineTypes.SQLite, sqlDefineAttr.DialectType, "Should be SQLite dialect");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void AllEComRepositories_ShouldHaveCorrectTableNames()
    {
        // Arrange & Act & Assert
        var expectedTableNames = new[]
        {
            (typeof(EComProductRepository), "ecom_products"),
            (typeof(EComCartRepository), "ecom_carts"),
            (typeof(EComCartItemRepository), "ecom_cart_items"),
            (typeof(EComOrderRepository), "ecom_orders")
        };

        foreach (var (repoType, expectedTableName) in expectedTableNames)
        {
            var tableNameAttr = repoType.GetCustomAttribute<TableNameAttribute>();
            Assert.IsNotNull(tableNameAttr, $"{repoType.Name} should have TableName attribute");
            Assert.AreEqual(expectedTableName, tableNameAttr.TableName, 
                $"{repoType.Name} should have table name '{expectedTableName}'");
        }
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void EComProductRepository_GetActiveProductsAsync_ShouldFilterByIsActive()
    {
        // Arrange
        var interfaceType = typeof(IEComProductRepository);
        var method = interfaceType.GetMethod("GetActiveProductsAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "GetActiveProductsAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("{{columns}}"), 
            "SQL should use {{columns}} placeholder");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("{{table}}"), 
            "SQL should use {{table}} placeholder");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("is_active = 1"), 
            "SQL should filter by is_active");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void EComProductRepository_GetProductsByCategoryAsync_ShouldFilterByCategory()
    {
        // Arrange
        var interfaceType = typeof(IEComProductRepository);
        var method = interfaceType.GetMethod("GetProductsByCategoryAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "GetProductsByCategoryAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("category = @category"), 
            "SQL should filter by category parameter");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void EComCartRepository_GetActiveCartByUserIdAsync_ShouldOrderByIdDesc()
    {
        // Arrange
        var interfaceType = typeof(IEComCartRepository);
        var method = interfaceType.GetMethod("GetActiveCartByUserIdAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "GetActiveCartByUserIdAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("ORDER BY id DESC"), 
            "SQL should order by id DESC");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("LIMIT 1"), 
            "SQL should limit to 1 result");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void EComCartItemRepository_ShouldHaveAllCrudOperations()
    {
        // Arrange
        var interfaceType = typeof(IEComCartItemRepository);
        
        // Act & Assert
        Assert.IsNotNull(interfaceType.GetMethod("AddItemAsync"), 
            "AddItemAsync should exist");
        Assert.IsNotNull(interfaceType.GetMethod("GetCartItemsAsync"), 
            "GetCartItemsAsync should exist");
        Assert.IsNotNull(interfaceType.GetMethod("UpdateQuantityAsync"), 
            "UpdateQuantityAsync should exist");
        Assert.IsNotNull(interfaceType.GetMethod("RemoveItemAsync"), 
            "RemoveItemAsync should exist");
        Assert.IsNotNull(interfaceType.GetMethod("ClearCartAsync"), 
            "ClearCartAsync should exist");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void EComOrderRepository_GetOrdersByUserIdAsync_ShouldOrderByOrderDateDesc()
    {
        // Arrange
        var interfaceType = typeof(IEComOrderRepository);
        var method = interfaceType.GetMethod("GetOrdersByUserIdAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "GetOrdersByUserIdAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("ORDER BY order_date DESC"), 
            "SQL should order by order_date DESC");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void EComOrderRepository_GetOrdersByUserIdAndStatusAsync_ShouldFilterByStatus()
    {
        // Arrange
        var interfaceType = typeof(IEComOrderRepository);
        var method = interfaceType.GetMethod("GetOrdersByUserIdAndStatusAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "GetOrdersByUserIdAndStatusAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("status = @status"), 
            "SQL should filter by status parameter");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void AllEComRepositories_ShouldUseSqlTemplateAttribute()
    {
        // Verify all interface methods use SqlTemplate attribute (not Sqlx)
        var interfaces = new[]
        {
            typeof(IEComProductRepository),
            typeof(IEComCartRepository),
            typeof(IEComCartItemRepository),
            typeof(IEComOrderRepository)
        };

        foreach (var interfaceType in interfaces)
        {
            var methods = interfaceType.GetMethods();
            foreach (var method in methods)
            {
                var sqlTemplateAttr = method.GetCustomAttribute<SqlTemplateAttribute>();
                Assert.IsNotNull(sqlTemplateAttr, 
                    $"{interfaceType.Name}.{method.Name} should have SqlTemplate attribute");
            }
        }
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void EComCartItemRepository_UpdateQuantityAsync_ShouldUseTablePlaceholder()
    {
        // Arrange
        var interfaceType = typeof(IEComCartItemRepository);
        var method = interfaceType.GetMethod("UpdateQuantityAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "UpdateQuantityAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("{{table}}"), 
            "SQL should use {{table}} placeholder");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("SET quantity = @quantity"), 
            "SQL should update quantity");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void EComCartItemRepository_DeleteOperations_ShouldUseTablePlaceholder()
    {
        // Arrange
        var interfaceType = typeof(IEComCartItemRepository);
        var removeMethod = interfaceType.GetMethod("RemoveItemAsync");
        var clearMethod = interfaceType.GetMethod("ClearCartAsync");
        
        // Act
        var removeAttr = removeMethod?.GetCustomAttribute<SqlTemplateAttribute>();
        var clearAttr = clearMethod?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(removeAttr, "RemoveItemAsync should have SqlTemplate attribute");
        Assert.IsTrue(removeAttr.Template.Contains("DELETE FROM {{table}}"), 
            "RemoveItemAsync should use {{table}} placeholder");
        
        Assert.IsNotNull(clearAttr, "ClearCartAsync should have SqlTemplate attribute");
        Assert.IsTrue(clearAttr.Template.Contains("DELETE FROM {{table}}"), 
            "ClearCartAsync should use {{table}} placeholder");
    }
}
