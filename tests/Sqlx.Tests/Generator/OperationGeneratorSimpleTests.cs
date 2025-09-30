// -----------------------------------------------------------------------
// <copyright file="OperationGeneratorSimpleTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;

namespace Sqlx.Tests.Generator;

/// <summary>
/// Simple tests for SqlTemplateEngine class.
/// </summary>
[TestClass]
public class OperationGeneratorSimpleTests : TestBase
{
    private SqlTemplateEngine _templateEngine = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _templateEngine = new SqlTemplateEngine();
    }

    [TestMethod]
    public void ValidateTemplate_ValidSql_ReturnsValid()
    {
        // Arrange
        var sql = "SELECT * FROM Users WHERE Id = @id";

        // Act
        var result = _templateEngine.ValidateTemplate(sql);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void ValidateTemplate_EmptyTemplate_ReturnsInvalid()
    {
        // Arrange
        var sql = "";

        // Act
        var result = _templateEngine.ValidateTemplate(sql);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Count > 0);
    }

    [TestMethod]
    public void ValidateTemplate_TemplateWithPlaceholders_ReturnsValid()
    {
        // Arrange
        var sql = "SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}";

        // Act
        var result = _templateEngine.ValidateTemplate(sql);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsValid);
    }
}

