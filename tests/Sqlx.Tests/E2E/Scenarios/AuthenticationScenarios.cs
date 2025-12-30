// -----------------------------------------------------------------------
// <copyright file="AuthenticationScenarios.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Integration;

namespace Sqlx.Tests.E2E.Scenarios;

/// <summary>
/// E2E tests for authentication scenarios.
/// **Validates: Requirements 2.1**
/// </summary>
public class AuthUser
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
}

public class AuthSession
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public partial interface IAuthUserRepository
{
    [SqlTemplate("INSERT INTO auth_users (username, email, password_hash, created_at, is_active) VALUES (@username, @email, @passwordHash, @createdAt, @isActive)")]
    Task<int> CreateUserAsync(string username, string email, string passwordHash, DateTime createdAt, bool isActive);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE username = @username")]
    Task<AuthUser?> GetByUsernameAsync(string username);

    [SqlTemplate("UPDATE {{table}} SET password_reset_token = @token, password_reset_expiry = @expiry WHERE id = @id")]
    Task<int> SetPasswordResetTokenAsync(long id, string token, DateTime expiry);

    [SqlTemplate("UPDATE {{table}} SET password_hash = @passwordHash, password_reset_token = NULL, password_reset_expiry = NULL WHERE id = @id")]
    Task<int> ResetPasswordAsync(long id, string passwordHash);

    [SqlTemplate("UPDATE {{table}} SET last_login_at = @loginTime WHERE id = @id")]
    Task<int> UpdateLastLoginAsync(long id, DateTime loginTime);
}

public partial interface IAuthSessionRepository
{
    [SqlTemplate("INSERT INTO auth_sessions (user_id, session_token, created_at, expires_at) VALUES (@userId, @sessionToken, @createdAt, @expiresAt)")]
    Task<int> CreateSessionAsync(long userId, string sessionToken, DateTime createdAt, DateTime expiresAt);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE session_token = @sessionToken AND expires_at > @now")]
    Task<AuthSession?> GetValidSessionAsync(string sessionToken, DateTime now);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("auth_users")]
[RepositoryFor(typeof(IAuthUserRepository))]
public partial class AuthUserRepository(DbConnection connection) : IAuthUserRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("auth_sessions")]
[RepositoryFor(typeof(IAuthSessionRepository))]
public partial class AuthSessionRepository(DbConnection connection) : IAuthSessionRepository { }

/// <summary>
/// SQLite-specific authentication E2E tests.
/// </summary>
[TestClass]
public class AuthenticationScenarios_SQLite
{
    private DatabaseFixture _fixture = null!;

    [TestInitialize]
    public void Initialize()
    {
        _fixture = new DatabaseFixture();
        _fixture.SeedAuthenticationData(SqlDefineTypes.SQLite);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture?.Dispose();
    }

    [TestMethod]
    public async Task UserRegistration_ShouldCreateUser()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new AuthUserRepository(connection);
        var username = "newuser";
        var email = "newuser@example.com";
        var passwordHash = "hashed_password";

        // Act
        var result = await repo.CreateUserAsync(username, email, passwordHash, DateTime.UtcNow, true);

        // Assert
        Assert.AreEqual(1, result, "Should insert 1 user");
        var user = await repo.GetByUsernameAsync(username);
        Assert.IsNotNull(user, "User should be created");
        Assert.AreEqual(username, user.Username);
        Assert.AreEqual(email, user.Email);
    }

    [TestMethod]
    public async Task UserLogin_ShouldCreateSession()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var userRepo = new AuthUserRepository(connection);
        var sessionRepo = new AuthSessionRepository(connection);
        
        var user = await userRepo.GetByUsernameAsync("testuser1");
        Assert.IsNotNull(user, "Test user should exist");

        // Act
        var sessionToken = Guid.NewGuid().ToString();
        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt.AddHours(24);
        var result = await sessionRepo.CreateSessionAsync(user.Id, sessionToken, createdAt, expiresAt);

        // Assert
        Assert.AreEqual(1, result, "Should create 1 session");
        var session = await sessionRepo.GetValidSessionAsync(sessionToken, DateTime.UtcNow);
        Assert.IsNotNull(session, "Session should be created");
        Assert.AreEqual(user.Id, session.UserId);
    }

    [TestMethod]
    public async Task PasswordReset_ShouldUpdateToken()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var repo = new AuthUserRepository(connection);
        var user = await repo.GetByUsernameAsync("testuser1");
        Assert.IsNotNull(user, "Test user should exist");

        // Act
        var resetToken = Guid.NewGuid().ToString();
        var expiry = DateTime.UtcNow.AddHours(1);
        var result = await repo.SetPasswordResetTokenAsync(user.Id, resetToken, expiry);

        // Assert
        Assert.AreEqual(1, result, "Should update 1 user");
    }
}
