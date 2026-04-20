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

public interface IValidatedUserRepository : ICrudRepository<ValidatedUser, long>
{
    [SqlTemplate("SELECT COUNT(*) FROM validated_users WHERE name = @name")]
    Task<int> CountByNameAsync([Required, StringLength(5)] string name, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT COUNT(*) FROM validated_users WHERE name = @name", ValidateParameters = false)]
    Task<int> CountByNameWithoutValidationAsync([Required, StringLength(5)] string name, CancellationToken cancellationToken = default);
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
            )";
        await command.ExecuteNonQueryAsync();

        _repository = new ValidatedUserRepository(_connection, SqlDefine.SQLite);
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
}
