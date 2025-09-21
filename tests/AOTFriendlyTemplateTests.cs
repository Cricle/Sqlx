using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Tests
{
    /// <summary>
    /// AOT友好SQL模板测试 - 验证完全兼容Native AOT
    /// </summary>
    [TestClass]
    public class AOTFriendlyTemplateTests
    {
        [TestMethod]
        public void SqlV2_Execute_WithDictionaryParameters_ShouldWork()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = {id} AND Name = {name}";
            var parameters = new Dictionary<string, object?>
            {
                ["id"] = 123,
                ["name"] = "张三"
            };

            // Act
            var result = SqlV2.Execute(sql, parameters);

            // Assert
            Assert.AreEqual("SELECT * FROM Users WHERE Id = 123 AND Name = '张三'", result);
        }

        [TestMethod]
        public void SqlV2_Execute_WithoutParameters_ShouldReturnOriginal()
        {
            // Arrange
            var sql = "SELECT COUNT(*) FROM Users";

            // Act
            var result = SqlV2.Execute(sql);

            // Assert
            Assert.AreEqual(sql, result);
        }

        [TestMethod]
        public void SqlParams_FluentBuilder_ShouldWork()
        {
            // Arrange & Act
            var parameters = SqlParams.New()
                .Add("age", 25)
                .Add("city", "北京")
                .Add("active", true);

            var result = SqlV2.Execute("SELECT * FROM Users WHERE Age = {age} AND City = {city} AND IsActive = {active}", parameters);

            // Assert
            Assert.AreEqual("SELECT * FROM Users WHERE Age = 25 AND City = '北京' AND IsActive = 1", result);
        }

        [TestMethod]
        public void SimpleTemplate_ChainedSet_ShouldWork()
        {
            // Arrange & Act
            var result = SqlV2.Template("UPDATE Products SET Price = {price}, Category = {category} WHERE Id = {id}")
                .Set("price", 199.99m)
                .Set("category", "电子产品")
                .Set("id", 456)
                .ToSql();

            // Assert
            Assert.AreEqual("UPDATE Products SET Price = 199.99, Category = '电子产品' WHERE Id = 456", result);
        }

        [TestMethod]
        public void SimpleTemplate_With_Dictionary_ShouldWork()
        {
            // Arrange
            var template = SqlV2.Template("DELETE FROM Orders WHERE UserId = {userId} AND Status = {status}");
            var parameters = new Dictionary<string, object?>
            {
                ["userId"] = 789,
                ["status"] = "Cancelled"
            };

            // Act
            var result = template.With(parameters).ToSql();

            // Assert
            Assert.AreEqual("DELETE FROM Orders WHERE UserId = 789 AND Status = 'Cancelled'", result);
        }

        [TestMethod]
        public void StringExtension_AsSimpleTemplate_ShouldWork()
        {
            // Arrange
            var template = "INSERT INTO Users (Name, Email) VALUES ({name}, {email})";

            // Act
            var result = template.AsSimpleTemplate()
                .Set("name", "李四")
                .Set("email", "lisi@example.com")
                .ToSql();

            // Assert
            Assert.AreEqual("INSERT INTO Users (Name, Email) VALUES ('李四', 'lisi@example.com')", result);
        }

        [TestMethod]
        public void StringExtension_SqlWith_ShouldWork()
        {
            // Arrange
            var template = "SELECT * FROM Orders WHERE Date > {date}";
            var parameters = SqlParams.New().Add("date", new DateTime(2024, 1, 1));

            // Act
            var result = template.SqlWith(parameters);

            // Assert
            Assert.AreEqual("SELECT * FROM Orders WHERE Date > '2024-01-01 00:00:00'", result);
        }

        [TestMethod]
        public void SqlV2_Batch_WithMultipleParameters_ShouldWork()
        {
            // Arrange
            var template = "INSERT INTO Products (Id, Name, Price) VALUES ({id}, {name}, {price})";
            var parametersList = new[]
            {
                new Dictionary<string, object?> { ["id"] = 1, ["name"] = "产品1", ["price"] = 100.50m },
                new Dictionary<string, object?> { ["id"] = 2, ["name"] = "产品2", ["price"] = 200.75m }
            };

            // Act
            var results = SqlV2.Batch(template, parametersList).ToList();

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("INSERT INTO Products (Id, Name, Price) VALUES (1, '产品1', 100.5)", results[0]);
            Assert.AreEqual("INSERT INTO Products (Id, Name, Price) VALUES (2, '产品2', 200.75)", results[1]);
        }

        [TestMethod]
        public void SimpleTemplate_ToParameterized_ShouldWork()
        {
            // Arrange
            var template = SqlV2.Template("SELECT * FROM Users WHERE Age > {age} AND City = {city}");
            var parameters = SqlParams.New()
                .Add("age", 18)
                .Add("city", "北京");

            // Act
            var parameterized = template.With(parameters).ToParameterized();

            // Assert
            Assert.AreEqual("SELECT * FROM Users WHERE Age > @age AND City = @city", parameterized.Sql);
            Assert.AreEqual(2, parameterized.Parameters.Count);
            Assert.AreEqual(18, parameterized.Parameters["@age"]);
            Assert.AreEqual("北京", parameterized.Parameters["@city"]);
        }

        [TestMethod]
        public void SimpleTemplate_WithNullValues_ShouldHandleCorrectly()
        {
            // Arrange
            var template = SqlV2.Template("SELECT * FROM Users WHERE Name = {name} AND Email = {email}");
            var parameters = SqlParams.New()
                .Add("name", null)
                .Add("email", "test@example.com");

            // Act
            var result = template.With(parameters).ToSql();

            // Assert
            Assert.AreEqual("SELECT * FROM Users WHERE Name = NULL AND Email = 'test@example.com'", result);
        }

        [TestMethod]
        public void SimpleTemplate_WithBooleanValues_ShouldFormatCorrectly()
        {
            // Arrange
            var template = SqlV2.Template("SELECT * FROM Users WHERE IsActive = {isActive} AND IsAdmin = {isAdmin}");
            var parameters = SqlParams.New()
                .Add("isActive", true)
                .Add("isAdmin", false);

            // Act
            var result = template.With(parameters).ToSql();

            // Assert
            Assert.AreEqual("SELECT * FROM Users WHERE IsActive = 1 AND IsAdmin = 0", result);
        }

        [TestMethod]
        public void SimpleTemplate_WithDateTimeValues_ShouldFormatCorrectly()
        {
            // Arrange
            var template = SqlV2.Template("SELECT * FROM Orders WHERE CreatedDate = {date}");
            var date = new DateTime(2024, 12, 25, 15, 30, 45);
            var parameters = SqlParams.New().Add("date", date);

            // Act
            var result = template.With(parameters).ToSql();

            // Assert
            Assert.AreEqual("SELECT * FROM Orders WHERE CreatedDate = '2024-12-25 15:30:45'", result);
        }

        [TestMethod]
        public void SimpleTemplate_WithGuidValues_ShouldFormatCorrectly()
        {
            // Arrange
            var template = SqlV2.Template("SELECT * FROM Users WHERE Id = {id}");
            var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
            var parameters = SqlParams.New().Add("id", guid);

            // Act
            var result = template.With(parameters).ToSql();

            // Assert
            Assert.AreEqual("SELECT * FROM Users WHERE Id = '12345678-1234-1234-1234-123456789012'", result);
        }

        [TestMethod]
        public void SimpleTemplate_WithDecimalValues_ShouldFormatCorrectly()
        {
            // Arrange
            var template = SqlV2.Template("SELECT * FROM Products WHERE Price = {price}");
            var parameters = SqlParams.New().Add("price", 123.456789m);

            // Act
            var result = template.With(parameters).ToSql();

            // Assert
            Assert.AreEqual("SELECT * FROM Products WHERE Price = 123.456789", result);
        }

        [TestMethod]
        public void SimpleTemplate_WithStringEscaping_ShouldHandleSingleQuotes()
        {
            // Arrange
            var template = SqlV2.Template("SELECT * FROM Users WHERE Name = {name}");
            var parameters = SqlParams.New().Add("name", "O'Connor");

            // Act
            var result = template.With(parameters).ToSql();

            // Assert
            Assert.AreEqual("SELECT * FROM Users WHERE Name = 'O''Connor'", result);
        }

        [TestMethod]
        public void SimpleTemplate_ImplicitStringConversion_ShouldWork()
        {
            // Arrange
            var template = SqlV2.Template("SELECT COUNT(*) FROM Users WHERE Age > {age}")
                .Set("age", 18);

            // Act - 隐式转换为字符串
            string result = template;

            // Assert
            Assert.AreEqual("SELECT COUNT(*) FROM Users WHERE Age > 18", result);
        }

        [TestMethod]
        public void SimpleTemplate_ToString_ShouldReturnSql()
        {
            // Arrange
            var template = SqlV2.Template("SELECT * FROM Products WHERE Category = {category}")
                .Set("category", "Books");

            // Act
            var result = template.ToString();

            // Assert
            Assert.AreEqual("SELECT * FROM Products WHERE Category = 'Books'", result);
        }

        [TestMethod]
        public void SimpleTemplate_WithEmptyParameters_ShouldReturnOriginalTemplate()
        {
            // Arrange
            var originalSql = "SELECT * FROM Users";
            var template = SqlV2.Template(originalSql);

            // Act
            var result = template.ToSql();

            // Assert
            Assert.AreEqual(originalSql, result);
        }

        [TestMethod]
        public void SqlParams_ImplicitConversion_ShouldWork()
        {
            // Arrange
            var sqlParams = SqlParams.New()
                .Add("id", 123)
                .Add("status", "Active");

            // Act - 隐式转换为Dictionary
            Dictionary<string, object?> dict = sqlParams;
            var result = SqlV2.Execute("SELECT * FROM Users WHERE Id = {id} AND Status = {status}", dict);

            // Assert
            Assert.AreEqual("SELECT * FROM Users WHERE Id = 123 AND Status = 'Active'", result);
        }

        [TestMethod]
        public void SimpleTemplate_ComplexQuery_ShouldWork()
        {
            // Arrange
            var template = @"
                SELECT u.Name, u.Email, COUNT(o.Id) as OrderCount
                FROM Users u
                LEFT JOIN Orders o ON u.Id = o.UserId
                WHERE u.Age BETWEEN {minAge} AND {maxAge}
                  AND u.City = {city}
                  AND u.IsActive = {isActive}
                GROUP BY u.Id, u.Name, u.Email
                HAVING COUNT(o.Id) > {minOrders}
                ORDER BY OrderCount DESC
                LIMIT {limit}";

            var parameters = SqlParams.New()
                .Add("minAge", 18)
                .Add("maxAge", 65)
                .Add("city", "上海")
                .Add("isActive", true)
                .Add("minOrders", 5)
                .Add("limit", 10);

            // Act
            var result = SqlV2.Template(template).With(parameters).ToSql();

            // Assert
            Assert.IsTrue(result.Contains("WHERE u.Age BETWEEN 18 AND 65"));
            Assert.IsTrue(result.Contains("AND u.City = '上海'"));
            Assert.IsTrue(result.Contains("AND u.IsActive = 1"));
            Assert.IsTrue(result.Contains("HAVING COUNT(o.Id) > 5"));
            Assert.IsTrue(result.Contains("LIMIT 10"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SqlV2_Template_WithNullTemplate_ShouldThrowException()
        {
            // Act & Assert
            SqlV2.Template(null!);
        }

        [TestMethod]
        public void SimpleTemplate_Performance_ShouldBeEfficient()
        {
            // Arrange
            var template = SqlV2.Template("SELECT * FROM Users WHERE Id = {id} AND Status = {status}");
            var parameters = SqlParams.New()
                .Add("id", 123)
                .Add("status", "Active");

            // Act & Assert - 性能测试：应该能快速执行1000次
            var startTime = DateTime.UtcNow;
            for (int i = 0; i < 1000; i++)
            {
                var result = template.With(parameters).ToSql();
                Assert.IsNotNull(result);
            }
            var elapsed = DateTime.UtcNow - startTime;

            // 1000次执行应该在1秒内完成
            Assert.IsTrue(elapsed.TotalSeconds < 1.0, $"Performance test failed: {elapsed.TotalMilliseconds}ms for 1000 operations");
        }

        [TestMethod]
        public void SqlParams_MultipleCalls_ShouldAccumulate()
        {
            // Arrange & Act
            var parameters = SqlParams.New()
                .Add("param1", "value1")
                .Add("param2", 123)
                .Add("param1", "updated_value1"); // 覆盖之前的值

            var dict = parameters.ToDictionary();

            // Assert
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual("updated_value1", dict["param1"]);
            Assert.AreEqual(123, dict["param2"]);
        }
    }
}
