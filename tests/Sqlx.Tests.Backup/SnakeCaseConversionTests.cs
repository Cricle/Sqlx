// <copyright file="SnakeCaseConversionTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.Linq;

namespace Sqlx.Tests;

/// <summary>
/// Comprehensive tests for snake_case conversion.
/// Tests all edge cases and scenarios for property name to column name conversion.
/// </summary>
[TestClass]
public class SnakeCaseConversionTests
{
    #region Basic Conversion Tests

    [TestMethod]
    public void SnakeCase_SingleWord_RemainsLowercase()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity1EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "Name");
        Assert.AreEqual("name", col.Name);
    }

    [TestMethod]
    public void SnakeCase_TwoWords_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity1EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "UserName");
        Assert.AreEqual("user_name", col.Name);
    }

    [TestMethod]
    public void SnakeCase_ThreeWords_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity1EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "FirstMiddleLast");
        Assert.AreEqual("first_middle_last", col.Name);
    }

    #endregion

    #region Acronym Tests

    [TestMethod]
    public void SnakeCase_AcronymAtStart_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity2EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "HTTPService");
        Assert.AreEqual("http_service", col.Name, "HTTPService should become http_service");
    }

    [TestMethod]
    public void SnakeCase_AcronymInMiddle_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity2EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "UseHTTPProtocol");
        Assert.AreEqual("use_http_protocol", col.Name, "UseHTTPProtocol should become use_http_protocol");
    }

    [TestMethod]
    public void SnakeCase_AcronymAtEnd_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity2EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "ServiceHTTP");
        Assert.AreEqual("service_http", col.Name, "ServiceHTTP should become service_http");
    }

    [TestMethod]
    public void SnakeCase_OnlyAcronym_RemainsLowercase()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity2EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "HTTP");
        Assert.AreEqual("http", col.Name, "HTTP should become http");
    }

    [TestMethod]
    public void SnakeCase_MultipleAcronyms_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity2EntityProvider.Default;
        
        // Assert
        // Note: HTTPSURLPath is ambiguous - could be HTTPS+URL+Path or HTTP+SURL+Path
        // Current implementation treats it as one acronym until lowercase: httpsurl_path
        var col = provider.Columns.First(c => c.PropertyName == "HTTPSURLPath");
        Assert.AreEqual("httpsurl_path", col.Name, "HTTPSURLPath becomes httpsurl_path (连续大写作为一个单词)");
    }

    #endregion

    #region Number Tests

    [TestMethod]
    public void SnakeCase_NumberAtEnd_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity3EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "Address2");
        Assert.AreEqual("address2", col.Name, "Address2 should become address2");
    }

    [TestMethod]
    public void SnakeCase_NumberInMiddle_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity3EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "Line1Text");
        Assert.AreEqual("line1_text", col.Name, "Line1Text should become line1_text");
    }

    [TestMethod]
    public void SnakeCase_MultipleNumbers_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity3EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "Code123Value");
        Assert.AreEqual("code123_value", col.Name, "Code123Value should become code123_value");
    }

    #endregion

    #region Single Character Tests

    [TestMethod]
    public void SnakeCase_SingleUpperCase_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity4EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "X");
        Assert.AreEqual("x", col.Name, "X should become x");
    }

    [TestMethod]
    public void SnakeCase_SingleLowerCase_RemainsUnchanged()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity4EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "y");
        Assert.AreEqual("y", col.Name, "y should remain y");
    }

    [TestMethod]
    public void SnakeCase_SingleCharWithWord_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity4EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "XCoordinate");
        Assert.AreEqual("x_coordinate", col.Name, "XCoordinate should become x_coordinate");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void SnakeCase_AlreadyLowerCase_RemainsUnchanged()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity5EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "id");
        Assert.AreEqual("id", col.Name, "id should remain id");
    }

    [TestMethod]
    public void SnakeCase_AllUpperCase_ConvertsToLowerCase()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity5EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "ID");
        Assert.AreEqual("id", col.Name, "ID should become id");
    }

    [TestMethod]
    public void SnakeCase_MixedCaseAcronym_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity5EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "XMLParser");
        Assert.AreEqual("xml_parser", col.Name, "XMLParser should become xml_parser");
    }

    [TestMethod]
    public void SnakeCase_ConsecutiveUpperThenLower_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity5EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "IOError");
        Assert.AreEqual("io_error", col.Name, "IOError should become io_error");
    }

    #endregion

    #region Real World Examples

    [TestMethod]
    public void SnakeCase_RealWorld_UserId_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity6EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "UserId");
        Assert.AreEqual("user_id", col.Name);
    }

    [TestMethod]
    public void SnakeCase_RealWorld_CreatedAt_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity6EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "CreatedAt");
        Assert.AreEqual("created_at", col.Name);
    }

    [TestMethod]
    public void SnakeCase_RealWorld_IsActive_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity6EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "IsActive");
        Assert.AreEqual("is_active", col.Name);
    }

    [TestMethod]
    public void SnakeCase_RealWorld_APIKey_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity6EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "APIKey");
        Assert.AreEqual("api_key", col.Name);
    }

    [TestMethod]
    public void SnakeCase_RealWorld_URLPath_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity6EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "URLPath");
        Assert.AreEqual("url_path", col.Name);
    }

    [TestMethod]
    public void SnakeCase_RealWorld_JSONData_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity6EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "JSONData");
        Assert.AreEqual("json_data", col.Name);
    }

    #endregion

    #region Complex Scenarios

    [TestMethod]
    public void SnakeCase_Complex_HTTPSURLWithPort_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity7EntityProvider.Default;
        
        // Assert
        // Note: HTTPSURL is treated as one acronym until lowercase letter
        var col = provider.Columns.First(c => c.PropertyName == "HTTPSURLWithPort");
        Assert.AreEqual("httpsurl_with_port", col.Name);
    }

    [TestMethod]
    public void SnakeCase_Complex_GetHTTPResponseCode_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity7EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "GetHTTPResponseCode");
        Assert.AreEqual("get_http_response_code", col.Name);
    }

    [TestMethod]
    public void SnakeCase_Complex_OAuth2Token_ConvertsCorrectly()
    {
        // Arrange & Act
        var provider = SnakeCaseEntity7EntityProvider.Default;
        
        // Assert
        var col = provider.Columns.First(c => c.PropertyName == "OAuth2Token");
        Assert.AreEqual("o_auth2_token", col.Name);
    }

    #endregion
}

#region Test Entities

[Sqlx]
public class SnakeCaseEntity1
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FirstMiddleLast { get; set; } = string.Empty;
}

[Sqlx]
public class SnakeCaseEntity2
{
    public int Id { get; set; }
    public string HTTPService { get; set; } = string.Empty;
    public string UseHTTPProtocol { get; set; } = string.Empty;
    public string ServiceHTTP { get; set; } = string.Empty;
    public string HTTP { get; set; } = string.Empty;
    public string HTTPSURLPath { get; set; } = string.Empty;
}

[Sqlx]
public class SnakeCaseEntity3
{
    public int Id { get; set; }
    public string Address2 { get; set; } = string.Empty;
    public string Line1Text { get; set; } = string.Empty;
    public string Code123Value { get; set; } = string.Empty;
}

[Sqlx]
public class SnakeCaseEntity4
{
    public int Id { get; set; }
    public string X { get; set; } = string.Empty;
    public string y { get; set; } = string.Empty;
    public string XCoordinate { get; set; } = string.Empty;
}

[Sqlx]
public class SnakeCaseEntity5
{
    public int id { get; set; }
    public string ID { get; set; } = string.Empty;
    public string XMLParser { get; set; } = string.Empty;
    public string IOError { get; set; } = string.Empty;
}

[Sqlx]
public class SnakeCaseEntity6
{
    public int UserId { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string APIKey { get; set; } = string.Empty;
    public string URLPath { get; set; } = string.Empty;
    public string JSONData { get; set; } = string.Empty;
}

[Sqlx]
public class SnakeCaseEntity7
{
    public int Id { get; set; }
    public string HTTPSURLWithPort { get; set; } = string.Empty;
    public string GetHTTPResponseCode { get; set; } = string.Empty;
    public string OAuth2Token { get; set; } = string.Empty;
}

#endregion
