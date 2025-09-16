// -----------------------------------------------------------------------
// <copyright file="CrudAdvancedOperationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// CRUD操作的高级功能测试，验证复杂的INSERT、UPDATE、DELETE和SELECT场景。
    /// </summary>
    [TestClass]
    public class CrudAdvancedOperationTests
    {
        #region 测试实体

        public class Product
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public decimal Price { get; set; }
            public int CategoryId { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool IsActive { get; set; }
            public int StockQuantity { get; set; }
            public string? Tags { get; set; }
        }

        public class Order
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
            public string? Status { get; set; }
            public string? ShippingAddress { get; set; }
        }

        #endregion

        #region SELECT操作高级测试

        [TestMethod]
        public void Select_CustomColumns_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Select(p => new { p.Id, p.Name, p.Price })
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Custom SELECT SQL: {sql}");
            Assert.IsTrue(sql.Contains("SELECT"), "应包含SELECT关键字");
            Assert.IsTrue(sql.Contains("Id") && sql.Contains("Name") && sql.Contains("Price"), 
                "应包含指定的列");
            Assert.IsTrue(sql.Contains("WHERE") && sql.Contains("IsActive"), 
                "应包含WHERE条件");
        }

        [TestMethod]
        public void Select_MultipleSelectors_CombinesColumns()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Select(p => p.Id, p => p.Name, p => p.Price)
                .Where(p => p.CategoryId > 0);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Multiple selectors SQL: {sql}");
            Assert.IsTrue(sql.Contains("SELECT"), "应包含SELECT关键字");
            Assert.IsTrue(sql.Contains("Id") && sql.Contains("Name") && sql.Contains("Price"), 
                "应包含所有指定的列");
        }

        [TestMethod]
        public void Select_WithComplexWhere_GeneratesCorrectSql()
        {
            // Arrange
            var minPrice = 100m;
            var categoryId1 = 1;
            var categoryId2 = 2;

            // Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Select(p => new { p.Id, p.Name, ProductPrice = p.Price })
                .Where(p => p.Price >= minPrice)
                .Where(p => p.CategoryId == categoryId1 || p.CategoryId == categoryId2)
                .Where(p => p.IsActive && p.StockQuantity > 0)
                .OrderBy(p => p.Price)
                .Take(20);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Complex SELECT with WHERE SQL: {sql}");
            Assert.IsTrue(sql.Contains("SELECT"), "应包含SELECT");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE");
            Assert.IsTrue(sql.Contains("ORDER BY"), "应包含ORDER BY");
            Assert.IsTrue(sql.Contains("Price") && sql.Contains("CategoryId") && 
                         sql.Contains("IsActive") && sql.Contains("StockQuantity"), 
                "应包含所有查询条件字段");
        }

        #endregion

        #region INSERT操作高级测试

        [TestMethod]
        public void Insert_SingleRow_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.InsertInto()
                .Values(1, "Test Product", "Test Description", 99.99m, 
                       1, DateTime.Now, null, true, 10, "tag1,tag2");

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Single INSERT SQL: {sql}");
            Assert.IsTrue(sql.Contains("INSERT INTO"), "应包含INSERT INTO");
            Assert.IsTrue(sql.Contains("VALUES"), "应包含VALUES");
            Assert.IsTrue(sql.Contains("Product"), "应包含表名");
        }

        [TestMethod]
        public void Insert_MultipleRows_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.InsertInto()
                .Values(1, "Product 1", "Desc 1", 10.99m, 1, DateTime.Now, null, true, 5, "tag1")
                .AddValues(2, "Product 2", "Desc 2", 20.99m, 2, DateTime.Now, null, true, 10, "tag2")
                .AddValues(3, "Product 3", "Desc 3", 30.99m, 3, DateTime.Now, null, false, 0, "tag3");

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Multiple INSERT SQL: {sql}");
            Assert.IsTrue(sql.Contains("INSERT INTO"), "应包含INSERT INTO");
            Assert.IsTrue(sql.Contains("VALUES"), "应包含VALUES");
            
            // 计算VALUES子句的数量
            var valuesCount = sql.Split('(').Length - 1;
            Assert.IsTrue(valuesCount >= 3, $"应包含至少3个VALUES子句，实际: {valuesCount}");
        }

        [TestMethod]
        public void Insert_LargeDataset_HandlesEfficiently()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.InsertInto();

            var startTime = DateTime.Now;
            
            // 插入大量数据
            for (int i = 1; i <= 1000; i++)
            {
                expr.AddValues(i, $"Product {i}", $"Description {i}", i * 10.99m, 
                              i % 10 + 1, DateTime.Now, null, i % 2 == 0, i * 5, $"tag{i}");
            }

            var sql = expr.ToSql();
            var endTime = DateTime.Now;

            // Assert
            var duration = endTime - startTime;
            Console.WriteLine($"Generated 1000-row INSERT in {duration.TotalMilliseconds}ms");
            Console.WriteLine($"SQL length: {sql.Length} characters");
            
            Assert.IsTrue(duration.TotalMilliseconds < 5000, "生成1000行INSERT应在5秒内完成");
            Assert.IsTrue(sql.Contains("INSERT INTO"), "应包含INSERT INTO");
            Assert.IsTrue(sql.Length > 50000, "应生成相当长的SQL");
        }

        [TestMethod]
        public void Insert_WithSelectClause_GeneratesCorrectSql()
        {
            // 注意：这个测试演示概念，但实际的INSERT FROM SELECT实现可能不同
            // Arrange & Act
            using var selectExpr = ExpressionToSql<Product>.ForSqlServer();
            selectExpr.Where(p => p.IsActive && p.StockQuantity > 0);
            var selectSql = selectExpr.ToSql();

            using var insertExpr = ExpressionToSql<Product>.ForSqlServer();
            insertExpr.InsertInto()
                     .Values(1, "Test Product", "Test Description", 99.99m, 
                            1, DateTime.Now, null, true, 10, "tag1");

            var insertSql = insertExpr.ToSql();

            // Assert
            Console.WriteLine($"SELECT SQL: {selectSql}");
            Console.WriteLine($"INSERT SQL: {insertSql}");
            Assert.IsTrue(selectSql.Contains("WHERE"), "SELECT应包含WHERE子句");
            Assert.IsTrue(insertSql.Contains("INSERT INTO"), "应包含INSERT INTO");
            Assert.IsTrue(insertSql.Contains("VALUES"), "应包含VALUES子句");
        }

        #endregion

        #region UPDATE操作高级测试

        [TestMethod]
        public void Update_BasicSet_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Set(p => p.Name, "Updated Name")
                .Set(p => p.Price, 199.99m)
                .Set(p => p.UpdatedAt, DateTime.Now)
                .Where(p => p.Id == 1);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Basic UPDATE SQL: {sql}");
            Assert.IsTrue(sql.Contains("UPDATE"), "应包含UPDATE");
            Assert.IsTrue(sql.Contains("SET"), "应包含SET");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE");
            Assert.IsTrue(sql.Contains("Name") && sql.Contains("Price") && sql.Contains("UpdatedAt"), 
                "应包含所有更新字段");
        }

        [TestMethod]
        public void Update_ExpressionSet_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Set(p => p.Price, p => p.Price * 1.1m)  // 价格增加10%
                .Set(p => p.StockQuantity, p => p.StockQuantity - 1)  // 库存减1
                .Set(p => p.UpdatedAt, DateTime.Now)
                .Where(p => p.CategoryId == 1);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Expression SET UPDATE SQL: {sql}");
            Assert.IsTrue(sql.Contains("UPDATE"), "应包含UPDATE");
            Assert.IsTrue(sql.Contains("SET"), "应包含SET");
            Assert.IsTrue(sql.Contains("*") && sql.Contains("-"), "应包含数学运算");
            Assert.IsTrue(sql.Contains("Price") && sql.Contains("StockQuantity"), 
                "应包含表达式更新字段");
        }

        [TestMethod]
        public void Update_ConditionalSet_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Set(p => p.IsActive, false)
                .Set(p => p.UpdatedAt, DateTime.Now)
                .Where(p => p.StockQuantity == 0)
                .Where(p => p.CreatedAt < DateTime.Now.AddDays(-30));

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Conditional UPDATE SQL: {sql}");
            Assert.IsTrue(sql.Contains("UPDATE"), "应包含UPDATE");
            Assert.IsTrue(sql.Contains("IsActive") && sql.Contains("UpdatedAt"), 
                "应包含更新字段");
            Assert.IsTrue(sql.Contains("StockQuantity") && sql.Contains("CreatedAt"), 
                "应包含条件字段");
        }

        [TestMethod]
        public void Update_MixedSetTypes_GeneratesCorrectSql()
        {
            // Arrange
            var newDescription = "Updated Description";

            // Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Set(p => p.Name, "Fixed Name")  // 常量设置
                .Set(p => p.Description, newDescription)  // 变量设置
                .Set(p => p.Price, p => p.Price + 10)  // 表达式设置
                .Set(p => p.UpdatedAt, DateTime.Now)  // 函数调用设置
                .Where(p => p.Id > 0);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Mixed SET types UPDATE SQL: {sql}");
            Assert.IsTrue(sql.Contains("UPDATE"), "应包含UPDATE");
            Assert.IsTrue(sql.Contains("Name") && sql.Contains("Description") && 
                         sql.Contains("Price") && sql.Contains("UpdatedAt"), 
                "应包含所有SET字段");
        }

        #endregion

        #region DELETE操作高级测试

        [TestMethod]
        public void Delete_BasicDelete_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Delete()
                .Where(p => p.Id == 1);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Basic DELETE SQL: {sql}");
            Assert.IsTrue(sql.Contains("DELETE FROM"), "应包含DELETE FROM");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE子句");
            Assert.IsTrue(sql.Contains("Id"), "应包含ID条件");
        }

        [TestMethod]
        public void Delete_ConditionalDelete_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Delete(p => p.IsActive == false && p.StockQuantity == 0);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Conditional DELETE SQL: {sql}");
            Assert.IsTrue(sql.Contains("DELETE FROM"), "应包含DELETE FROM");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE子句");
            Assert.IsTrue(sql.Contains("IsActive") && sql.Contains("StockQuantity"), 
                "应包含删除条件");
        }

        [TestMethod]
        public void Delete_MultipleConditions_GeneratesCorrectSql()
        {
            // Arrange
            var cutoffDate = DateTime.Now.AddYears(-1);

            // Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Delete()
                .Where(p => p.IsActive == false)
                .Where(p => p.StockQuantity == 0)
                .Where(p => p.CreatedAt < cutoffDate);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Multiple conditions DELETE SQL: {sql}");
            Assert.IsTrue(sql.Contains("DELETE FROM"), "应包含DELETE FROM");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE子句");
            Assert.IsTrue(sql.Contains("IsActive") && sql.Contains("StockQuantity") && 
                         sql.Contains("CreatedAt"), "应包含所有删除条件");
        }

        [TestMethod]
        public void Delete_WithoutWhere_ThrowsException()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Delete();

            // Assert
            try
            {
                var sql = expr.ToSql();
                Assert.Fail("没有WHERE条件的DELETE应该抛出异常");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✅ 正确抛出异常: {ex.Message}");
                Assert.IsTrue(ex.Message.Contains("WHERE"), "异常消息应提到WHERE子句");
            }
        }

        #endregion

        #region 复杂CRUD组合测试

        [TestMethod]
        public void CRUD_ComplexScenario_AllOperations()
        {
            // 这个测试验证各种CRUD操作的SQL生成，不实际执行
            var testResults = new List<string>();

            // SELECT测试
            using (var selectExpr = ExpressionToSql<Product>.ForSqlServer())
            {
                selectExpr.Select(p => new { p.Id, p.Name, p.Price })
                         .Where(p => p.IsActive && p.Price > 10)
                         .OrderBy(p => p.Name)
                         .Take(10);
                
                testResults.Add($"SELECT: {selectExpr.ToSql()}");
            }

            // INSERT测试
            using (var insertExpr = ExpressionToSql<Product>.ForSqlServer())
            {
                insertExpr.InsertInto()
                         .Values(1, "Test", "Desc", 99.99m, 1, DateTime.Now, null, true, 10, "tags");
                
                testResults.Add($"INSERT: {insertExpr.ToSql()}");
            }

            // UPDATE测试
            using (var updateExpr = ExpressionToSql<Product>.ForSqlServer())
            {
                updateExpr.Set(p => p.Price, 199.99m)
                         .Set(p => p.UpdatedAt, DateTime.Now)
                         .Where(p => p.Id == 1);
                
                testResults.Add($"UPDATE: {updateExpr.ToSql()}");
            }

            // DELETE测试
            using (var deleteExpr = ExpressionToSql<Product>.ForSqlServer())
            {
                deleteExpr.Delete()
                         .Where(p => p.Id == 1);
                
                testResults.Add($"DELETE: {deleteExpr.ToSql()}");
            }

            // Assert
            foreach (var result in testResults)
            {
                Console.WriteLine(result);
                Assert.IsTrue(result.Length > 10, "每个SQL语句都应有合理长度");
            }

            Assert.AreEqual(4, testResults.Count, "应生成4个不同的CRUD操作SQL");
        }

        [TestMethod]
        public void CRUD_TransactionScenario_MultipleOperations()
        {
            // 模拟事务场景：创建订单并更新库存
            var operations = new List<string>();

            // 1. 检查库存
            using (var checkStock = ExpressionToSql<Product>.ForSqlServer())
            {
                checkStock.Select(p => new { p.Id, p.StockQuantity })
                         .Where(p => p.Id == 1 && p.StockQuantity > 0);
                
                operations.Add($"Check Stock: {checkStock.ToSql()}");
            }

            // 2. 创建订单
            using (var createOrder = ExpressionToSql<Order>.ForSqlServer())
            {
                createOrder.InsertInto()
                          .Values(1, 100, DateTime.Now, 99.99m, "Pending", "123 Main St");
                
                operations.Add($"Create Order: {createOrder.ToSql()}");
            }

            // 3. 更新库存
            using (var updateStock = ExpressionToSql<Product>.ForSqlServer())
            {
                updateStock.Set(p => p.StockQuantity, p => p.StockQuantity - 1)
                          .Set(p => p.UpdatedAt, DateTime.Now)
                          .Where(p => p.Id == 1);
                
                operations.Add($"Update Stock: {updateStock.ToSql()}");
            }

            // Assert
            foreach (var operation in operations)
            {
                Console.WriteLine(operation);
                Assert.IsTrue(operation.Length > 20, "每个操作都应生成有意义的SQL");
            }

            Assert.AreEqual(3, operations.Count, "应生成3个事务操作");
        }

        #endregion

        #region 性能优化测试

        [TestMethod]
        public void CRUD_MemoryUsage_EfficientHandling()
        {
            // 测试大量CRUD操作的内存使用情况
            var initialMemory = GC.GetTotalMemory(true);
            
            // 执行多个CRUD操作
            for (int i = 0; i < 100; i++)
            {
                using var expr = ExpressionToSql<Product>.ForSqlServer();
                expr.Where(p => p.Id == i)
                    .OrderBy(p => p.Name)
                    .Take(10);
                
                var sql = expr.ToSql();
                // 确保SQL被生成
                Assert.IsTrue(sql.Length > 0);
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = finalMemory - initialMemory;
            
            Console.WriteLine($"Memory used for 100 CRUD operations: {memoryUsed} bytes");
            Assert.IsTrue(memoryUsed < 1024 * 1024, "100个CRUD操作应使用少于1MB内存");
        }

        [TestMethod]
        public void CRUD_ReuseExpressions_PerformanceGain()
        {
            // 测试表达式重用的性能影响
            var reusableExpression = ExpressionToSql<Product>.ForSqlServer();
            
            var startTime = DateTime.Now;
            
            // 多次使用同一个表达式构建器
            for (int i = 0; i < 50; i++)
            {
                reusableExpression.Where(p => p.CategoryId == i);
            }
            
            var sql = reusableExpression.ToSql();
            var endTime = DateTime.Now;
            
            reusableExpression.Dispose();
            
            var duration = endTime - startTime;
            Console.WriteLine($"50 WHERE conditions added in {duration.TotalMilliseconds}ms");
            Console.WriteLine($"Final SQL length: {sql.Length}");
            
            Assert.IsTrue(duration.TotalMilliseconds < 100, "50个条件应在100ms内添加完成");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE子句");
        }

        #endregion
    }
}
