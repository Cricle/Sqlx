// -----------------------------------------------------------------------
// <copyright file="SqlTemplateNewDesignTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// 测试重构后的SqlTemplate新设计：分离模板定义与参数值
    /// 验证"模板是模板，参数是参数"的设计原则
    /// </summary>
    [TestClass]
    public class SqlTemplateNewDesignTests
    {
        [TestMethod]
        public void SqlTemplate_Parse_CreatesPureTemplate()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @id";

            // Act
            var template = SqlTemplate.Parse(sql);

            // Assert
            Assert.AreEqual(sql, template.Sql);
            Assert.IsTrue(template.IsPureTemplate, "Should be a pure template without parameters");
            Assert.AreEqual(0, template.Parameters.Count, "Pure template should have no parameters");
        }

        [TestMethod]
        public void SqlTemplate_Execute_WithAnonymousObject_CreatesParameterizedSql()
        {
            // Arrange
            var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id AND IsActive = @isActive");
            var parameters = new { id = 123, isActive = true };

            // Act
            var execution = template.Execute(parameters);

            // Assert
            Assert.AreEqual(template.Sql, execution.Sql);
            Assert.AreEqual(2, execution.Parameters.Count);
            Assert.AreEqual(123, execution.Parameters["id"]);
            Assert.AreEqual(true, execution.Parameters["isActive"]);
        }

        [TestMethod]
        public void SqlTemplate_Execute_WithDictionary_CreatesParameterizedSql()
        {
            // Arrange
            var template = SqlTemplate.Parse("SELECT * FROM Products WHERE CategoryId = @categoryId");
            var parameters = new Dictionary<string, object?> { ["categoryId"] = 42 };

            // Act
            var execution = template.Execute(parameters);

            // Assert
            Assert.AreEqual(template.Sql, execution.Sql);
            Assert.AreEqual(1, execution.Parameters.Count);
            Assert.AreEqual(42, execution.Parameters["categoryId"]);
        }

        [TestMethod]
        public void SqlTemplate_Bind_FluentApi_WorksCorrectly()
        {
            // Arrange
            var template = SqlTemplate.Parse(@"
                SELECT * FROM Orders 
                WHERE CustomerId = @customerId 
                AND OrderDate >= @startDate 
                AND TotalAmount >= @minAmount");

            // Act
            var execution = template.Bind()
                .Param("customerId", 123)
                .Param("startDate", new DateTime(2023, 1, 1))
                .Param("minAmount", 100.50m)
                .Build();

            // Assert
            Assert.AreEqual(template.Sql, execution.Sql);
            Assert.AreEqual(3, execution.Parameters.Count);
            Assert.AreEqual(123, execution.Parameters["customerId"]);
            Assert.AreEqual(new DateTime(2023, 1, 1), execution.Parameters["startDate"]);
            Assert.AreEqual(100.50m, execution.Parameters["minAmount"]);
        }

        [TestMethod]
        public void SqlTemplate_Reuse_SameTemplateMultipleExecutions()
        {
            // Arrange
            var template = SqlTemplate.Parse("SELECT * FROM Users WHERE DepartmentId = @deptId");

            // Act
            var execution1 = template.Execute(new { deptId = 1 });
            var execution2 = template.Execute(new { deptId = 2 });
            var execution3 = template.Execute(new { deptId = 3 });

            // Assert
            // All executions should have the same SQL template
            Assert.AreEqual(template.Sql, execution1.Sql);
            Assert.AreEqual(template.Sql, execution2.Sql);
            Assert.AreEqual(template.Sql, execution3.Sql);

            // But different parameter values
            Assert.AreEqual(1, execution1.Parameters["deptId"]);
            Assert.AreEqual(2, execution2.Parameters["deptId"]);
            Assert.AreEqual(3, execution3.Parameters["deptId"]);

            // Template should remain pure
            Assert.IsTrue(template.IsPureTemplate);
        }

        [TestMethod]
        public void ParameterizedSql_Create_WorksWithAnonymousObject()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Age > @age";
            var parameters = new { age = 25 };

            // Act
            var parameterizedSql = ParameterizedSql.Create(sql, parameters);

            // Assert
            Assert.AreEqual(sql, parameterizedSql.Sql);
            Assert.AreEqual(1, parameterizedSql.Parameters.Count);
            Assert.AreEqual(25, parameterizedSql.Parameters["age"]);
        }

        [TestMethod]
        public void ParameterizedSql_CreateWithDictionary_WorksCorrectly()
        {
            // Arrange
            var sql = "SELECT * FROM Products WHERE Price BETWEEN @minPrice AND @maxPrice";
            var parameters = new Dictionary<string, object?> 
            { 
                ["minPrice"] = 10.0m, 
                ["maxPrice"] = 100.0m 
            };

            // Act
            var parameterizedSql = ParameterizedSql.CreateWithDictionary(sql, parameters);

            // Assert
            Assert.AreEqual(sql, parameterizedSql.Sql);
            Assert.AreEqual(2, parameterizedSql.Parameters.Count);
            Assert.AreEqual(10.0m, parameterizedSql.Parameters["minPrice"]);
            Assert.AreEqual(100.0m, parameterizedSql.Parameters["maxPrice"]);
        }

        [TestMethod]
        public void ParameterizedSql_Render_InlinesParameters()
        {
            // Arrange
            var parameterizedSql = ParameterizedSql.Create(
                "SELECT * FROM Users WHERE Name = @name AND Age = @age", 
                new { name = "John", age = 30 });

            // Act
            var renderedSql = parameterizedSql.Render();

            // Assert
            Assert.IsTrue(renderedSql.Contains("'John'"));
            Assert.IsTrue(renderedSql.Contains("30"));
            Assert.IsFalse(renderedSql.Contains("@name"));
            Assert.IsFalse(renderedSql.Contains("@age"));
        }

        [TestMethod]
        public void SqlTemplateBuilder_Params_BatchParameterBinding()
        {
            // Arrange
            var template = SqlTemplate.Parse("SELECT * FROM Orders WHERE Status = @status AND Amount > @amount");
            var batchParams = new { status = "Completed", amount = 500.0m };

            // Act
            var execution = template.Bind()
                .Params(batchParams)
                .Build();

            // Assert
            Assert.AreEqual(2, execution.Parameters.Count);
            Assert.AreEqual("Completed", execution.Parameters["status"]);
            Assert.AreEqual(500.0m, execution.Parameters["amount"]);
        }

        [TestMethod]
        public void SqlTemplate_PerformanceComparison_OldVsNewDesign()
        {
            // This test demonstrates the performance advantage of the new design
            
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @id";
            
            // Old design (create new template each time) - should be slower
            var oldExecutions = new List<SqlTemplate>();
            var startOld = DateTime.Now;
            
            #pragma warning disable CS0618 // Ignore obsolete warning for demo
            for (int i = 0; i < 1000; i++)
            {
                oldExecutions.Add(SqlTemplate.Create(sql, new { id = i }));
            }
            #pragma warning restore CS0618
            
            var oldTime = DateTime.Now - startOld;

            // New design (reuse template) - should be faster
            var template = SqlTemplate.Parse(sql);
            var newExecutions = new List<ParameterizedSql>();
            var startNew = DateTime.Now;
            
            for (int i = 0; i < 1000; i++)
            {
                newExecutions.Add(template.Execute(new { id = i }));
            }
            
            var newTime = DateTime.Now - startNew;

            // Assert
            Assert.AreEqual(1000, oldExecutions.Count);
            Assert.AreEqual(1000, newExecutions.Count);
            
            // The new design should be faster (though this might vary based on system performance)
            Console.WriteLine($"Old design time: {oldTime.TotalMilliseconds}ms");
            Console.WriteLine($"New design time: {newTime.TotalMilliseconds}ms");
            Console.WriteLine($"Performance improvement: {((oldTime.TotalMilliseconds - newTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100):F1}%");
            
            // Template should remain pure after 1000 executions
            Assert.IsTrue(template.IsPureTemplate);
        }

        [TestMethod]
        public void SqlTemplate_NullParameters_HandledCorrectly()
        {
            // Arrange
            var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Name = @name");

            // Act
            var execution = template.Execute(new { name = (string?)null });

            // Assert
            Assert.AreEqual(1, execution.Parameters.Count);
            Assert.IsNull(execution.Parameters["name"]);
            
            var renderedSql = execution.Render();
            Assert.IsTrue(renderedSql.Contains("NULL"));
        }

        [TestMethod]
        public void SqlTemplate_EmptyParameters_HandledCorrectly()
        {
            // Arrange
            var template = SqlTemplate.Parse("SELECT COUNT(*) FROM Users");

            // Act
            var execution = template.Execute();

            // Assert
            Assert.AreEqual(0, execution.Parameters.Count);
            Assert.AreEqual(template.Sql, execution.Render());
        }

        [TestMethod]
        public void SqlTemplate_ComplexObject_ExtractsPropertiesCorrectly()
        {
            // Arrange
            var template = SqlTemplate.Parse(@"
                SELECT * FROM Users 
                WHERE Name = @Name 
                AND Email = @Email 
                AND IsActive = @IsActive");

            var user = new 
            { 
                Name = "Test User", 
                Email = "test@example.com", 
                IsActive = true,
                IgnoredProperty = "This should not be included"
            };

            // Act
            var execution = template.Execute(user);

            // Assert
            Assert.AreEqual(4, execution.Parameters.Count); // All properties are extracted
            Assert.AreEqual("Test User", execution.Parameters["Name"]);
            Assert.AreEqual("test@example.com", execution.Parameters["Email"]);
            Assert.AreEqual(true, execution.Parameters["IsActive"]);
            Assert.AreEqual("This should not be included", execution.Parameters["IgnoredProperty"]);
        }
    }
}
