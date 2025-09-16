// -----------------------------------------------------------------------
// <copyright file="SqlTemplateComprehensiveTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable CS8765, CS8625, CS8604, CS8603, CS8602, CS8629 // Null-related warnings in test code

namespace Sqlx.Tests.Core;

using System;
using System.Data;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

/// <summary>
/// Mock DbParameter for testing SqlTemplate without external dependencies.
/// </summary>
public class MockDbParameter : DbParameter
{
    private DbType _dbType;
    private ParameterDirection _direction;
    
    public MockDbParameter(string parameterName, object? value)
    {
        ParameterName = parameterName;
        Value = value;
    }

    public override DbType DbType 
    { 
        get => _dbType; 
        set => _dbType = value; 
    }
    
    public override ParameterDirection Direction 
    { 
        get => _direction; 
        set => _direction = value; 
    }
    
    public override bool IsNullable { get; set; }
    public override string ParameterName { get; set; }
    public override int Size { get; set; }
    public override string SourceColumn { get; set; } = string.Empty;
    public override bool SourceColumnNullMapping { get; set; }
    public override object? Value { get; set; }

    public override void ResetDbType() { _dbType = DbType.String; }
}

/// <summary>
/// Comprehensive tests for SqlTemplate record struct.
/// </summary>
[TestClass]
public class SqlTemplateComprehensiveTests
{
    #region SqlTemplate Constructor Tests

    /// <summary>
    /// Tests SqlTemplate constructor with valid parameters.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_ConstructorWithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        const string sql = "SELECT * FROM Users WHERE Id = @id";
        var parameters = new DbParameter[] { new MockDbParameter("@id", 1) };

        // Act
        var template = new SqlTemplate(sql, parameters);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.AreSame(parameters, template.Parameters);
    }

    /// <summary>
    /// Tests SqlTemplate constructor with null SQL.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_ConstructorWithNullSql_SetsNullSql()
    {
        // Arrange
        var parameters = new DbParameter[] { new MockDbParameter("@id", 1) };

        // Act
        var template = new SqlTemplate(null!, parameters);

        // Assert
        Assert.IsNull(template.Sql);
        Assert.AreSame(parameters, template.Parameters);
    }

    /// <summary>
    /// Tests SqlTemplate constructor with null parameters.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_ConstructorWithNullParameters_SetsNullParameters()
    {
        // Arrange
        const string sql = "SELECT * FROM Users";

        // Act
        var template = new SqlTemplate(sql, null!);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.IsNull(template.Parameters);
    }

    /// <summary>
    /// Tests SqlTemplate constructor with empty parameters array.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_ConstructorWithEmptyParameters_SetsEmptyParametersArray()
    {
        // Arrange
        const string sql = "SELECT * FROM Users";
        var parameters = new DbParameter[0];

        // Act
        var template = new SqlTemplate(sql, parameters);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.AreSame(parameters, template.Parameters);
        Assert.AreEqual(0, template.Parameters.Length);
    }

    /// <summary>
    /// Tests SqlTemplate constructor with multiple parameters.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_ConstructorWithMultipleParameters_SetsParametersCorrectly()
    {
        // Arrange
        const string sql = "SELECT * FROM Users WHERE Id = @id AND Name = @name";
        var parameters = new DbParameter[]
        {
            new MockDbParameter("@id", 1),
            new MockDbParameter("@name", "John")
        };

        // Act
        var template = new SqlTemplate(sql, parameters);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.AreSame(parameters, template.Parameters);
        Assert.AreEqual(2, template.Parameters.Length);
        Assert.AreEqual("@id", template.Parameters[0].ParameterName);
        Assert.AreEqual("@name", template.Parameters[1].ParameterName);
    }

    #endregion

    #region SqlTemplate Equality Tests

    /// <summary>
    /// Tests SqlTemplate equality with identical values.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_EqualityWithIdenticalValues_ReturnsTrue()
    {
        // Arrange
        const string sql = "SELECT * FROM Users";
        var parameters = new DbParameter[] { new MockDbParameter("@id", 1) };
        var template1 = new SqlTemplate(sql, parameters);
        var template2 = new SqlTemplate(sql, parameters);

        // Act & Assert
        Assert.AreEqual(template1, template2);
        Assert.IsTrue(template1 == template2);
        Assert.IsFalse(template1 != template2);
    }

    /// <summary>
    /// Tests SqlTemplate equality with different SQL.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_EqualityWithDifferentSql_ReturnsFalse()
    {
        // Arrange
        var parameters = new DbParameter[] { new MockDbParameter("@id", 1) };
        var template1 = new SqlTemplate("SELECT * FROM Users", parameters);
        var template2 = new SqlTemplate("SELECT * FROM Orders", parameters);

        // Act & Assert
        Assert.AreNotEqual(template1, template2);
        Assert.IsFalse(template1 == template2);
        Assert.IsTrue(template1 != template2);
    }

    /// <summary>
    /// Tests SqlTemplate equality with different parameters.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_EqualityWithDifferentParameters_ReturnsFalse()
    {
        // Arrange
        const string sql = "SELECT * FROM Users WHERE Id = @id";
        var parameters1 = new DbParameter[] { new MockDbParameter("@id", 1) };
        var parameters2 = new DbParameter[] { new MockDbParameter("@id", 2) };
        var template1 = new SqlTemplate(sql, parameters1);
        var template2 = new SqlTemplate(sql, parameters2);

        // Act & Assert
        Assert.AreNotEqual(template1, template2);
        Assert.IsFalse(template1 == template2);
        Assert.IsTrue(template1 != template2);
    }

    /// <summary>
    /// Tests SqlTemplate equality with null values.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_EqualityWithNullValues_WorksCorrectly()
    {
        // Arrange
        var template1 = new SqlTemplate(null!, null!);
        var template2 = new SqlTemplate(null!, null!);
        var template3 = new SqlTemplate("SELECT 1", null!);

        // Act & Assert
        Assert.AreEqual(template1, template2);
        Assert.AreNotEqual(template1, template3);
    }

    #endregion

    #region SqlTemplate GetHashCode Tests

    /// <summary>
    /// Tests SqlTemplate GetHashCode with identical values.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_GetHashCodeWithIdenticalValues_ReturnsSameHashCode()
    {
        // Arrange
        const string sql = "SELECT * FROM Users";
        var parameters = new DbParameter[] { new MockDbParameter("@id", 1) };
        var template1 = new SqlTemplate(sql, parameters);
        var template2 = new SqlTemplate(sql, parameters);

        // Act
        var hashCode1 = template1.GetHashCode();
        var hashCode2 = template2.GetHashCode();

        // Assert
        Assert.AreEqual(hashCode1, hashCode2);
    }

    /// <summary>
    /// Tests SqlTemplate GetHashCode with different values.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_GetHashCodeWithDifferentValues_ReturnsDifferentHashCodes()
    {
        // Arrange
        var template1 = new SqlTemplate("SELECT * FROM Users", new DbParameter[0]);
        var template2 = new SqlTemplate("SELECT * FROM Orders", new DbParameter[0]);

        // Act
        var hashCode1 = template1.GetHashCode();
        var hashCode2 = template2.GetHashCode();

        // Assert
        Assert.AreNotEqual(hashCode1, hashCode2);
    }

    #endregion

    #region SqlTemplate ToString Tests

    /// <summary>
    /// Tests SqlTemplate ToString method.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_ToString_ReturnsNonEmptyString()
    {
        // Arrange
        const string sql = "SELECT * FROM Users";
        var parameters = new DbParameter[] { new MockDbParameter("@id", 1) };
        var template = new SqlTemplate(sql, parameters);

        // Act
        var result = template.ToString();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    /// <summary>
    /// Tests SqlTemplate ToString with null values.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_ToStringWithNullValues_DoesNotThrow()
    {
        // Arrange
        var template = new SqlTemplate(null!, null!);

        // Act & Assert - Should not throw
        var result = template.ToString();
        Assert.IsNotNull(result);
    }

    #endregion

    #region SqlTemplate Deconstruction Tests

    /// <summary>
    /// Tests SqlTemplate deconstruction.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_Deconstruction_ExtractsValuesCorrectly()
    {
        // Arrange
        const string sql = "SELECT * FROM Users";
        var parameters = new DbParameter[] { new MockDbParameter("@id", 1) };
        var template = new SqlTemplate(sql, parameters);

        // Act
        var (extractedSql, extractedParameters) = template;

        // Assert
        Assert.AreEqual(sql, extractedSql);
        Assert.AreSame(parameters, extractedParameters);
    }

    #endregion

    #region SqlTemplate Immutability Tests

    /// <summary>
    /// Tests SqlTemplate is immutable (record struct behavior).
    /// </summary>
    [TestMethod]
    public void SqlTemplate_IsImmutable_CannotModifyAfterCreation()
    {
        // Arrange
        const string sql = "SELECT * FROM Users";
        var parameters = new DbParameter[] { new MockDbParameter("@id", 1) };
        var template = new SqlTemplate(sql, parameters);

        // Act & Assert
        // Properties should be readonly - this is enforced at compile time
        Assert.AreEqual(sql, template.Sql);
        Assert.AreSame(parameters, template.Parameters);
        
        // Verify the record struct nature
        Assert.IsTrue(template.GetType().IsValueType);
    }

    #endregion

    #region SqlTemplate with operator Tests

    /// <summary>
    /// Tests SqlTemplate with operator for creating modified copies.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_WithOperator_CreatesModifiedCopy()
    {
        // Arrange
        const string originalSql = "SELECT * FROM Users";
        const string newSql = "SELECT * FROM Orders";
        var originalParameters = new DbParameter[] { new MockDbParameter("@id", 1) };
        var newParameters = new DbParameter[] { new MockDbParameter("@orderId", 2) };
        var original = new SqlTemplate(originalSql, originalParameters);

        // Act
        var modifiedSql = original with { Sql = newSql };
        var modifiedParameters = original with { Parameters = newParameters };
        var modifiedBoth = original with { Sql = newSql, Parameters = newParameters };

        // Assert
        Assert.AreEqual(newSql, modifiedSql.Sql);
        Assert.AreSame(originalParameters, modifiedSql.Parameters);
        
        Assert.AreEqual(originalSql, modifiedParameters.Sql);
        Assert.AreSame(newParameters, modifiedParameters.Parameters);
        
        Assert.AreEqual(newSql, modifiedBoth.Sql);
        Assert.AreSame(newParameters, modifiedBoth.Parameters);
        
        // Original should be unchanged
        Assert.AreEqual(originalSql, original.Sql);
        Assert.AreSame(originalParameters, original.Parameters);
    }

    #endregion

    #region SqlTemplate Edge Cases

    /// <summary>
    /// Tests SqlTemplate with very long SQL string.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_WithVeryLongSql_HandlesCorrectly()
    {
        // Arrange
        var longSql = new string('A', 10000); // 10,000 character string
        var parameters = new DbParameter[0];

        // Act
        var template = new SqlTemplate(longSql, parameters);

        // Assert
        Assert.AreEqual(longSql, template.Sql);
        Assert.AreEqual(10000, template.Sql.Length);
    }

    /// <summary>
    /// Tests SqlTemplate with large number of parameters.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_WithManyParameters_HandlesCorrectly()
    {
        // Arrange
        const string sql = "SELECT * FROM Users WHERE 1=1";
        var parameters = new DbParameter[1000];
        for (int i = 0; i < 1000; i++)
        {
            parameters[i] = new MockDbParameter($"@param{i}", i);
        }

        // Act
        var template = new SqlTemplate(sql, parameters);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.AreEqual(1000, template.Parameters.Length);
        Assert.AreEqual("@param0", template.Parameters[0].ParameterName);
        Assert.AreEqual("@param999", template.Parameters[999].ParameterName);
    }

    /// <summary>
    /// Tests SqlTemplate with empty SQL string.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_WithEmptySql_HandlesCorrectly()
    {
        // Arrange
        var parameters = new DbParameter[] { new MockDbParameter("@id", 1) };

        // Act
        var template = new SqlTemplate(string.Empty, parameters);

        // Assert
        Assert.AreEqual(string.Empty, template.Sql);
        Assert.AreSame(parameters, template.Parameters);
    }

    /// <summary>
    /// Tests SqlTemplate with whitespace-only SQL string.
    /// </summary>
    [TestMethod]
    public void SqlTemplate_WithWhitespaceSql_HandlesCorrectly()
    {
        // Arrange
        const string whitespaceSql = "   \t\n\r   ";
        var parameters = new DbParameter[0];

        // Act
        var template = new SqlTemplate(whitespaceSql, parameters);

        // Assert
        Assert.AreEqual(whitespaceSql, template.Sql);
        Assert.AreSame(parameters, template.Parameters);
    }

    #endregion
}