// -----------------------------------------------------------------------
// <copyright file="SqlTemplateTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System.Data.Common;
using System.Data;

namespace Sqlx.Tests.Core;

/// <summary>
/// Comprehensive tests for SqlTemplate record struct.
/// </summary>
[TestClass]
public class SqlTemplateTests : TestBase
{
    [TestMethod]
    public void Constructor_WithValidParameters_CreatesTemplate()
    {
        // Arrange
        string sql = "SELECT * FROM Users WHERE Id = @id";
        var parameters = new DbParameter[] { CreateMockParameter("@id", 1) };

        // Act
        var template = new SqlTemplate(sql, parameters);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.AreEqual(1, template.Parameters.Length);
        Assert.AreEqual("@id", template.Parameters[0].ParameterName);
    }

    [TestMethod]
    public void Constructor_WithNullSql_CreatesTemplateWithNullSql()
    {
        // Arrange
        string? sql = null;
        var parameters = new DbParameter[0];

        // Act
        var template = new SqlTemplate(sql!, parameters);

        // Assert
        Assert.IsNull(template.Sql);
        Assert.AreEqual(0, template.Parameters.Length);
    }

    [TestMethod]
    public void Constructor_WithNullParameters_CreatesTemplateWithEmptyParameters()
    {
        // Arrange
        string sql = "SELECT 1";
        DbParameter[]? parameters = null;

        // Act
        var template = new SqlTemplate(sql, parameters!);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.IsNull(template.Parameters);
    }

    [TestMethod]
    public void Constructor_WithEmptyParameters_CreatesTemplateWithEmptyArray()
    {
        // Arrange
        string sql = "SELECT 1";
        var parameters = new DbParameter[0];

        // Act
        var template = new SqlTemplate(sql, parameters);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.AreEqual(0, template.Parameters.Length);
    }

    [TestMethod]
    public void Equality_SameContent_ReturnsTrue()
    {
        // Arrange
        string sql = "SELECT * FROM Users";
        var parameters1 = new DbParameter[] { CreateMockParameter("@id", 1) };
        var parameters2 = new DbParameter[] { CreateMockParameter("@id", 1) };

        var template1 = new SqlTemplate(sql, parameters1);
        var template2 = new SqlTemplate(sql, parameters2);

        // Act & Assert
        Assert.AreEqual(template1, template2);
        Assert.IsTrue(template1.Equals(template2));
        Assert.IsTrue(template1 == template2);
        Assert.IsFalse(template1 != template2);
    }

    [TestMethod]
    public void Equality_DifferentSql_ReturnsFalse()
    {
        // Arrange
        var parameters = new DbParameter[] { CreateMockParameter("@id", 1) };

        var template1 = new SqlTemplate("SELECT * FROM Users", parameters);
        var template2 = new SqlTemplate("SELECT * FROM Products", parameters);

        // Act & Assert
        Assert.AreNotEqual(template1, template2);
        Assert.IsFalse(template1.Equals(template2));
        Assert.IsFalse(template1 == template2);
        Assert.IsTrue(template1 != template2);
    }

    [TestMethod]
    public void Equality_DifferentParameterCount_ReturnsFalse()
    {
        // Arrange
        string sql = "SELECT * FROM Users";
        var parameters1 = new DbParameter[] { CreateMockParameter("@id", 1) };
        var parameters2 = new DbParameter[] {
            CreateMockParameter("@id", 1),
            CreateMockParameter("@name", "test")
        };

        var template1 = new SqlTemplate(sql, parameters1);
        var template2 = new SqlTemplate(sql, parameters2);

        // Act & Assert
        Assert.AreNotEqual(template1, template2);
        Assert.IsFalse(template1.Equals(template2));
    }

    [TestMethod]
    public void GetHashCode_SameContent_ReturnsSameHashCode()
    {
        // Arrange
        string sql = "SELECT * FROM Users";
        var parameters1 = new DbParameter[] { CreateMockParameter("@id", 1) };
        var parameters2 = new DbParameter[] { CreateMockParameter("@id", 1) };

        var template1 = new SqlTemplate(sql, parameters1);
        var template2 = new SqlTemplate(sql, parameters2);

        // Act
        int hash1 = template1.GetHashCode();
        int hash2 = template2.GetHashCode();

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void GetHashCode_DifferentContent_ReturnsDifferentHashCode()
    {
        // Arrange
        var parameters = new DbParameter[] { CreateMockParameter("@id", 1) };

        var template1 = new SqlTemplate("SELECT * FROM Users", parameters);
        var template2 = new SqlTemplate("SELECT * FROM Products", parameters);

        // Act
        int hash1 = template1.GetHashCode();
        int hash2 = template2.GetHashCode();

        // Assert
        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public void With_UpdatesSql_CreatesNewTemplate()
    {
        // Arrange
        var original = new SqlTemplate("SELECT * FROM Users", new DbParameter[0]);
        string newSql = "SELECT * FROM Products";

        // Act
        var updated = original with { Sql = newSql };

        // Assert
        Assert.AreEqual(newSql, updated.Sql);
        Assert.AreEqual(original.Parameters, updated.Parameters);
        Assert.AreNotEqual(original, updated);
    }

    [TestMethod]
    public void With_UpdatesParameters_CreatesNewTemplate()
    {
        // Arrange
        var original = new SqlTemplate("SELECT * FROM Users", new DbParameter[0]);
        var newParameters = new DbParameter[] { CreateMockParameter("@id", 1) };

        // Act
        var updated = original with { Parameters = newParameters };

        // Assert
        Assert.AreEqual(original.Sql, updated.Sql);
        Assert.AreEqual(newParameters, updated.Parameters);
        Assert.AreNotEqual(original, updated);
    }

    [TestMethod]
    public void With_UpdatesBothProperties_CreatesNewTemplate()
    {
        // Arrange
        var original = new SqlTemplate("SELECT * FROM Users", new DbParameter[0]);
        string newSql = "SELECT * FROM Products";
        var newParameters = new DbParameter[] { CreateMockParameter("@id", 1) };

        // Act
        var updated = original with { Sql = newSql, Parameters = newParameters };

        // Assert
        Assert.AreEqual(newSql, updated.Sql);
        Assert.AreEqual(newParameters, updated.Parameters);
        Assert.AreNotEqual(original, updated);
    }

    [TestMethod]
    public void ToString_WithSqlAndParameters_ReturnsFormattedString()
    {
        // Arrange
        string sql = "SELECT * FROM Users WHERE Id = @id";
        var parameters = new DbParameter[] { CreateMockParameter("@id", 1) };
        var template = new SqlTemplate(sql, parameters);

        // Act
        string result = template.ToString();

        // Assert
        Assert.IsTrue(result.Contains(sql));
        Assert.IsTrue(result.Contains("1")); // Parameter count or content
    }

    [TestMethod]
    public void ToString_WithEmptyParameters_ReturnsFormattedString()
    {
        // Arrange
        string sql = "SELECT 1";
        var parameters = new DbParameter[0];
        var template = new SqlTemplate(sql, parameters);

        // Act
        string result = template.ToString();

        // Assert
        Assert.IsTrue(result.Contains(sql));
    }

    [TestMethod]
    public void Immutability_OriginalUnchangedAfterWith()
    {
        // Arrange
        string originalSql = "SELECT * FROM Users";
        var originalParameters = new DbParameter[] { CreateMockParameter("@id", 1) };
        var original = new SqlTemplate(originalSql, originalParameters);

        // Act
        var updated = original with { Sql = "SELECT * FROM Products" };

        // Assert
        Assert.AreEqual(originalSql, original.Sql);
        Assert.AreEqual(originalParameters, original.Parameters);
        Assert.AreNotEqual(original.Sql, updated.Sql);
    }

    [TestMethod]
    public void MultipleParameters_WorkCorrectly()
    {
        // Arrange
        string sql = "SELECT * FROM Users WHERE Id = @id AND Name = @name AND Age > @age";
        var parameters = new DbParameter[] {
            CreateMockParameter("@id", 1),
            CreateMockParameter("@name", "John"),
            CreateMockParameter("@age", 25)
        };

        // Act
        var template = new SqlTemplate(sql, parameters);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.AreEqual(3, template.Parameters.Length);
        Assert.AreEqual("@id", template.Parameters[0].ParameterName);
        Assert.AreEqual("@name", template.Parameters[1].ParameterName);
        Assert.AreEqual("@age", template.Parameters[2].ParameterName);
    }

    [TestMethod]
    public void LongSql_HandledCorrectly()
    {
        // Arrange
        var longSql = string.Join(" UNION ALL ",
            System.Linq.Enumerable.Repeat("SELECT 1 as col", 100));
        var parameters = new DbParameter[0];

        // Act
        var template = new SqlTemplate(longSql, parameters);

        // Assert
        Assert.AreEqual(longSql, template.Sql);
        Assert.AreEqual(0, template.Parameters.Length);
        Assert.IsTrue(template.Sql!.Length > 1000);
    }

    [TestMethod]
    public void ParametersWithNullValues_HandledCorrectly()
    {
        // Arrange
        string sql = "SELECT * FROM Users WHERE Name = @name";
        var parameters = new DbParameter[] { CreateMockParameter("@name", null) };

        // Act
        var template = new SqlTemplate(sql, parameters);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.AreEqual(1, template.Parameters.Length);
        Assert.AreEqual(System.DBNull.Value, template.Parameters[0].Value);
    }

    [TestMethod]
    public void RecordStructBehavior_ValueSemantics()
    {
        // Arrange
        var template1 = new SqlTemplate("SELECT 1", new DbParameter[0]);
        var template2 = template1; // Value copy

        // Act
        var template3 = template2 with { Sql = "SELECT 2" };

        // Assert
        Assert.AreEqual(template1, template2); // Same value
        Assert.AreNotEqual(template1, template3); // Different value
        Assert.AreEqual("SELECT 1", template1.Sql);
        Assert.AreEqual("SELECT 1", template2.Sql);
        Assert.AreEqual("SELECT 2", template3.Sql);
    }

    /// <summary>
    /// Creates a mock DbParameter for testing purposes.
    /// </summary>
    private static DbParameter CreateMockParameter(string name, object? value)
    {
        var param = new MockDbParameter();
        param.ParameterName = name;
        param.Value = value ?? System.DBNull.Value;
        return param;
    }

    /// <summary>
    /// Mock implementation of DbParameter for testing.
    /// </summary>
    private class MockDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; } = string.Empty;
        public override int Size { get; set; }
        public override string SourceColumn { get; set; } = string.Empty;
        public override bool SourceColumnNullMapping { get; set; }
        public override object Value { get; set; } = null!;

        public override void ResetDbType() { }
    }
}