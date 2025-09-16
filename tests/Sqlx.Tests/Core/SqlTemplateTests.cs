// -----------------------------------------------------------------------
// <copyright file="SqlTemplateTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable CS8765, CS8625, CS8604, CS8603, CS8602, CS8629 // Null-related warnings in test code

namespace Sqlx.Tests.Core;

using System.Data;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

/// <summary>
/// Tests for SqlTemplate record struct.
/// </summary>
[TestClass]
public class SqlTemplateTests
{
    /// <summary>
    /// Creates a test DbParameter.
    /// </summary>
    private static DbParameter CreateTestParameter(string name, object value)
    {
        return new TestDbParameter(name, value);
    }

    /// <summary>
    /// Simple test implementation of DbParameter.
    /// </summary>
    private class TestDbParameter : DbParameter
    {
        public TestDbParameter(string name, object? value)
        {
            ParameterName = name;
            Value = value;
        }

        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; }
        public override int Size { get; set; }
        public override string SourceColumn { get; set; } = string.Empty;
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; }
        public override void ResetDbType() { }
    }
    /// <summary>
    /// Tests SqlTemplate constructor and properties.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var sql = "SELECT * FROM Users WHERE Id = @id";
        var parameters = new DbParameter[] 
        { 
            CreateTestParameter("@id", 1) 
        };

        // Act
        var template = new SqlTemplate(sql, parameters);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.AreSame(parameters, template.Parameters);
        Assert.AreEqual(1, template.Parameters.Length);
    }

    /// <summary>
    /// Tests SqlTemplate with empty parameters.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_EmptyParameters_WorksCorrectly()
    {
        // Arrange
        var sql = "SELECT COUNT(*) FROM Users";
        var parameters = new DbParameter[0];

        // Act
        var template = new SqlTemplate(sql, parameters);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.AreSame(parameters, template.Parameters);
        Assert.AreEqual(0, template.Parameters.Length);
    }

    /// <summary>
    /// Tests SqlTemplate equality comparison.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_Equality_WorksCorrectly()
    {
        // Arrange
        var sql = "SELECT * FROM Users";
        var parameters1 = new DbParameter[] { CreateTestParameter("@id", 1) };
        var parameters2 = new DbParameter[] { CreateTestParameter("@id", 1) };
        
        var template1 = new SqlTemplate(sql, parameters1);
        var template2 = new SqlTemplate(sql, parameters1); // Same reference
        var template3 = new SqlTemplate(sql, parameters2); // Different reference
        var template4 = new SqlTemplate("SELECT * FROM Orders", parameters1);

        // Act & Assert
        Assert.AreEqual(template1, template2); // Same reference should be equal
        Assert.AreNotEqual(template1, template3); // Different parameter reference
        Assert.AreNotEqual(template1, template4); // Different SQL
    }

    /// <summary>
    /// Tests SqlTemplate GetHashCode.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_GetHashCode_WorksCorrectly()
    {
        // Arrange
        var sql = "SELECT * FROM Users";
        var parameters = new DbParameter[] { CreateTestParameter("@id", 1) };
        var template1 = new SqlTemplate(sql, parameters);
        var template2 = new SqlTemplate(sql, parameters);

        // Act
        var hash1 = template1.GetHashCode();
        var hash2 = template2.GetHashCode();

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    /// <summary>
    /// Tests SqlTemplate ToString method.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var sql = "SELECT * FROM Users WHERE Id = @id";
        var parameters = new DbParameter[] { CreateTestParameter("@id", 1) };
        var template = new SqlTemplate(sql, parameters);

        // Act
        var result = template.ToString();

        // Assert
        Assert.IsTrue(result.Contains("SqlTemplate"));
        Assert.IsTrue(result.Contains(sql));
    }

    /// <summary>
    /// Tests SqlTemplate with null SQL.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_NullSql_AllowsNull()
    {
        // Arrange
        string? sql = null;
        var parameters = new DbParameter[0];

        // Act
        var template = new SqlTemplate(sql!, parameters);

        // Assert
        Assert.IsNull(template.Sql);
        Assert.AreSame(parameters, template.Parameters);
    }

    /// <summary>
    /// Tests SqlTemplate with null parameters.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_NullParameters_AllowsNull()
    {
        // Arrange
        var sql = "SELECT * FROM Users";
        DbParameter[]? parameters = null;

        // Act
        var template = new SqlTemplate(sql, parameters!);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.IsNull(template.Parameters);
    }
}
