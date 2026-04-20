// <copyright file="ValidationAttributeSupportTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Threading;

namespace Sqlx.Tests;

[Sqlx]
[TableName("validated_users")]
public class ValidatedUser
{
    [Key]
    public long Id { get; set; }

    [Required]
    [StringLength(5)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 120)]
    public int Age { get; set; }
}

[Sqlx]
[TableName("validated_profiles")]
[CustomValidation(typeof(ValidatedProfile), nameof(ValidatedProfile.ValidateProfile))]
public class ValidatedProfile : IValidatableObject
{
    [Key]
    public long Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public static ValidationResult? ValidateProfile(ValidatedProfile profile, ValidationContext _)
    {
        return profile.Role == "system" && !profile.Email.EndsWith("@internal.local", StringComparison.OrdinalIgnoreCase)
            ? new ValidationResult("System profiles must use an internal email.")
            : ValidationResult.Success;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role == "guest" && Email.EndsWith("@internal.local", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "Guest profiles cannot use internal emails.",
                new[] { nameof(Role), nameof(Email) });
        }
    }
}

[Sqlx]
[TableName("validated_credentials")]
public class ValidatedCredential
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MinLength(8)]
    [MaxLength(12)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [RegularExpression("^[A-Z0-9]+$")]
    public string AccessCode { get; set; } = string.Empty;
}

public interface IValidatedUserRepository : ICrudRepository<ValidatedUser, long>
{
    [SqlTemplate("SELECT COUNT(*) FROM validated_users WHERE name = @name")]
    Task<int> CountByNameAsync([Required, StringLength(5)] string name, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT COUNT(*) FROM validated_users WHERE name = @name", ValidateParameters = false)]
    Task<int> CountByNameWithoutValidationAsync([Required, StringLength(5)] string name, CancellationToken cancellationToken = default);

    [SqlTemplate("INSERT INTO validated_users (name, age) VALUES (@name, @age)", ValidateParameters = false)]
    Task<int> InsertUncheckedAsync(ValidatedUser user, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT COUNT(*) FROM validated_users WHERE name = @name AND @email IS NOT NULL")]
    Task<int> CountByNameAndEmailAsync(
        [Required, StringLength(5)] string name,
        [Required, EmailAddress] string email,
        CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT COUNT(*) FROM validated_users WHERE @name IS NOT NULL")]
    Task<int> CountByPatternAsync(
        [RegularExpression("^[a-z]{2,5}$")] string name,
        CancellationToken cancellationToken = default);
}

public interface IValidatedProfileRepository : ICrudRepository<ValidatedProfile, long>
{
}

public interface IValidatedCredentialRepository : ICrudRepository<ValidatedCredential, long>
{
}

[RepositoryFor(typeof(IValidatedProfileRepository), TableName = "validated_profiles")]
public partial class ValidatedProfileRepository : IValidatedProfileRepository
{
    private readonly DbConnection _connection;

    public ValidatedProfileRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[RepositoryFor(typeof(IValidatedCredentialRepository), TableName = "validated_credentials")]
public partial class ValidatedCredentialRepository : IValidatedCredentialRepository
{
    private readonly DbConnection _connection;

    public ValidatedCredentialRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[RepositoryFor(typeof(IValidatedUserRepository), TableName = "validated_users")]
public partial class ValidatedUserRepository : IValidatedUserRepository
{
    private readonly DbConnection _connection;

    public ValidatedUserRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[TestClass]
public class ValidationAttributeSupportTests
{
    private SqliteConnection _connection = null!;
    private ValidatedUserRepository _repository = null!;
    private ValidatedProfileRepository _profileRepository = null!;
    private ValidatedCredentialRepository _credentialRepository = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE validated_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                age INTEGER NOT NULL
            );

            CREATE TABLE validated_profiles (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                email TEXT NOT NULL,
                role TEXT NOT NULL
            );

            CREATE TABLE validated_credentials (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                password TEXT NOT NULL,
                confirm_password TEXT NOT NULL,
                access_code TEXT NOT NULL
            )";
        await command.ExecuteNonQueryAsync();

        _repository = new ValidatedUserRepository(_connection, SqlDefine.SQLite);
        _profileRepository = new ValidatedProfileRepository(_connection, SqlDefine.SQLite);
        _credentialRepository = new ValidatedCredentialRepository(_connection, SqlDefine.SQLite);
    }

    [TestCleanup]
    public async Task CleanupAsync()
    {
        await _connection.DisposeAsync();
    }

    [TestMethod]
    public void GeneratedParameterBinder_WithInvalidEntity_ThrowsValidationException()
    {
        var invalid = new ValidatedUser
        {
            Name = "toolong",
            Age = 0
        };

        using var command = new TestDbCommand();

        Assert.ThrowsException<ValidationException>(() =>
            ValidatedUserParameterBinder.Default.BindEntity(command, invalid));
    }

    [TestMethod]
    public async Task RepositoryInsertAsync_WithInvalidEntity_ThrowsValidationException()
    {
        var invalid = new ValidatedUser
        {
            Name = string.Empty,
            Age = 18
        };

        await Assert.ThrowsExceptionAsync<ValidationException>(async () =>
            await _repository.InsertAsync(invalid, CancellationToken.None));
    }

    [TestMethod]
    public async Task RepositoryMethod_WithInvalidAnnotatedParameter_ThrowsValidationException()
    {
        await Assert.ThrowsExceptionAsync<ValidationException>(async () =>
            await _repository.CountByNameAsync("toolong", CancellationToken.None));
    }

    [TestMethod]
    public async Task RepositoryMethod_WithValidationDisabled_SkipsValidation()
    {
        var count = await _repository.CountByNameWithoutValidationAsync("toolong", CancellationToken.None);

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task RepositoryEntityMethod_WithValidationDisabled_SkipsEntityValidation()
    {
        var invalid = new ValidatedUser
        {
            Name = string.Empty,
            Age = 0
        };

        var rows = await _repository.InsertUncheckedAsync(invalid, CancellationToken.None);

        Assert.AreEqual(1, rows);
    }

    [TestMethod]
    public async Task RepositoryMethod_WithEmailValidation_ThrowsValidationException()
    {
        await Assert.ThrowsExceptionAsync<ValidationException>(async () =>
            await _repository.CountByNameAndEmailAsync("admin", "not-an-email", CancellationToken.None));
    }

    [TestMethod]
    public async Task RepositoryInsertAsync_WithCustomValidationAttribute_ThrowsValidationException()
    {
        var invalid = new ValidatedProfile
        {
            Email = "ops@example.com",
            Role = "system"
        };

        await Assert.ThrowsExceptionAsync<ValidationException>(async () =>
            await _profileRepository.InsertAsync(invalid, CancellationToken.None));
    }

    [TestMethod]
    public async Task RepositoryInsertAsync_WithIValidatableObject_ThrowsValidationException()
    {
        var invalid = new ValidatedProfile
        {
            Email = "guest@internal.local",
            Role = "guest"
        };

        await Assert.ThrowsExceptionAsync<ValidationException>(async () =>
            await _profileRepository.InsertAsync(invalid, CancellationToken.None));
    }

    [TestMethod]
    public void GeneratedParameterBinder_WithCompareAttribute_ThrowsValidationException()
    {
        var invalid = new ValidatedCredential
        {
            Password = "Password1",
            ConfirmPassword = "Password2",
            AccessCode = "ACCESS01"
        };

        using var command = new TestDbCommand();

        Assert.ThrowsException<ValidationException>(() =>
            ValidatedCredentialParameterBinder.Default.BindEntity(command, invalid));
    }

    [TestMethod]
    public async Task RepositoryInsertAsync_WithCompareAttribute_ThrowsValidationException()
    {
        var invalid = new ValidatedCredential
        {
            Password = "Password1",
            ConfirmPassword = "Password2",
            AccessCode = "ACCESS01"
        };

        await Assert.ThrowsExceptionAsync<ValidationException>(async () =>
            await _credentialRepository.InsertAsync(invalid, CancellationToken.None));
    }

    [TestMethod]
    public async Task RepositoryInsertAsync_WithRegularExpressionAttribute_ThrowsValidationException()
    {
        var invalid = new ValidatedCredential
        {
            Password = "Password1",
            ConfirmPassword = "Password1",
            AccessCode = "lowercase"
        };

        await Assert.ThrowsExceptionAsync<ValidationException>(async () =>
            await _credentialRepository.InsertAsync(invalid, CancellationToken.None));
    }

    [TestMethod]
    public async Task RepositoryInsertAsync_WithMinLengthAttribute_ThrowsValidationException()
    {
        var invalid = new ValidatedCredential
        {
            Password = "short",
            ConfirmPassword = "short",
            AccessCode = "ACCESS01"
        };

        await Assert.ThrowsExceptionAsync<ValidationException>(async () =>
            await _credentialRepository.InsertAsync(invalid, CancellationToken.None));
    }

    [TestMethod]
    public async Task RepositoryMethod_WithRegularExpressionParameter_ThrowsValidationException()
    {
        await Assert.ThrowsExceptionAsync<ValidationException>(async () =>
            await _repository.CountByPatternAsync("ADMIN-01", CancellationToken.None));
    }
}
