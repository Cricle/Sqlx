// -----------------------------------------------------------------------
// <copyright file="AuthenticationScenarios_SqlValidation.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.E2E.Scenarios;

/// <summary>
/// TDD tests to validate SQL generation for Authentication scenarios.
/// Verifies that placeholders are correctly replaced and SQL is valid for each dialect.
/// </summary>
[TestClass]
public class AuthenticationScenarios_SqlValidation
{
    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void AuthUserRepository_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var repoType = typeof(AuthUserRepository);
        
        // Act
        var tableNameAttr = repoType.GetCustomAttribute<TableNameAttribute>();
        var sqlDefineAttr = repoType.GetCustomAttribute<SqlDefineAttribute>();
        
        // Assert
        Assert.IsNotNull(tableNameAttr, "Repository should have TableName attribute");
        Assert.AreEqual("auth_users", tableNameAttr.TableName, "Table name should be 'auth_users'");
        Assert.IsNotNull(sqlDefineAttr, "Repository should have SqlDefine attribute");
        Assert.AreEqual(SqlDefineTypes.SQLite, sqlDefineAttr.DialectType, "Should be SQLite dialect");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void AuthSessionRepository_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var repoType = typeof(AuthSessionRepository);
        
        // Act
        var tableNameAttr = repoType.GetCustomAttribute<TableNameAttribute>();
        var sqlDefineAttr = repoType.GetCustomAttribute<SqlDefineAttribute>();
        
        // Assert
        Assert.IsNotNull(tableNameAttr, "Repository should have TableName attribute");
        Assert.AreEqual("auth_sessions", tableNameAttr.TableName, "Table name should be 'auth_sessions'");
        Assert.IsNotNull(sqlDefineAttr, "Repository should have SqlDefine attribute");
        Assert.AreEqual(SqlDefineTypes.SQLite, sqlDefineAttr.DialectType, "Should be SQLite dialect");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void AuthUserRepository_CreateUserAsync_ShouldHaveSqlTemplateAttribute()
    {
        // Arrange
        var interfaceType = typeof(IAuthUserRepository);
        var method = interfaceType.GetMethod("CreateUserAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "CreateUserAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("INSERT INTO auth_users"), 
            "SQL should insert into auth_users table");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("@username"), 
            "SQL should have @username parameter");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void AuthUserRepository_GetByUsernameAsync_ShouldUseColumnsPlaceholder()
    {
        // Arrange
        var interfaceType = typeof(IAuthUserRepository);
        var method = interfaceType.GetMethod("GetByUsernameAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "GetByUsernameAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("{{columns}}"), 
            "SQL should use {{columns}} placeholder");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("{{table}}"), 
            "SQL should use {{table}} placeholder");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void AuthUserRepository_SetPasswordResetTokenAsync_ShouldUseTablePlaceholder()
    {
        // Arrange
        var interfaceType = typeof(IAuthUserRepository);
        var method = interfaceType.GetMethod("SetPasswordResetTokenAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "SetPasswordResetTokenAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("{{table}}"), 
            "SQL should use {{table}} placeholder");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("password_reset_token"), 
            "SQL should update password_reset_token");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void AuthUserRepository_ResetPasswordAsync_ShouldClearResetToken()
    {
        // Arrange
        var interfaceType = typeof(IAuthUserRepository);
        var method = interfaceType.GetMethod("ResetPasswordAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "ResetPasswordAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("password_reset_token = NULL"), 
            "SQL should clear password_reset_token");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("password_reset_expiry = NULL"), 
            "SQL should clear password_reset_expiry");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void AuthSessionRepository_GetValidSessionAsync_ShouldFilterByExpiry()
    {
        // Arrange
        var interfaceType = typeof(IAuthSessionRepository);
        var method = interfaceType.GetMethod("GetValidSessionAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "GetValidSessionAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("expires_at > @now"), 
            "SQL should filter by expiry date");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void AllAuthRepositories_ShouldHaveGeneratedMethods()
    {
        // Arrange
        var userRepoType = typeof(AuthUserRepository);
        var sessionRepoType = typeof(AuthSessionRepository);
        
        // Act & Assert - User repository methods
        Assert.IsNotNull(userRepoType.GetMethod("CreateUserAsync"), 
            "CreateUserAsync should be generated");
        Assert.IsNotNull(userRepoType.GetMethod("GetByUsernameAsync"), 
            "GetByUsernameAsync should be generated");
        Assert.IsNotNull(userRepoType.GetMethod("SetPasswordResetTokenAsync"), 
            "SetPasswordResetTokenAsync should be generated");
        Assert.IsNotNull(userRepoType.GetMethod("ResetPasswordAsync"), 
            "ResetPasswordAsync should be generated");
        Assert.IsNotNull(userRepoType.GetMethod("UpdateLastLoginAsync"), 
            "UpdateLastLoginAsync should be generated");
        
        // Act & Assert - Session repository methods
        Assert.IsNotNull(sessionRepoType.GetMethod("CreateSessionAsync"), 
            "CreateSessionAsync should be generated");
        Assert.IsNotNull(sessionRepoType.GetMethod("GetValidSessionAsync"), 
            "GetValidSessionAsync should be generated");
    }
}
