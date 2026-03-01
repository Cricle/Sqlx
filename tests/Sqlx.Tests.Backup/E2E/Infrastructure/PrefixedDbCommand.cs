// <copyright file="PrefixedDbCommand.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Data;
using System.Data.Common;

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// A wrapper around DbCommand that automatically applies table prefixes to SQL statements.
/// This ensures test isolation when using shared databases.
/// </summary>
public class PrefixedDbCommand : DbCommand
{
    private readonly DbCommand _innerCommand;
    private readonly DatabaseFixture _fixture;
    private string _commandText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefixedDbCommand"/> class.
    /// </summary>
    /// <param name="innerCommand">The inner command to wrap.</param>
    /// <param name="fixture">The database fixture providing the table prefix.</param>
    public PrefixedDbCommand(DbCommand innerCommand, DatabaseFixture fixture)
    {
        _innerCommand = innerCommand;
        _fixture = fixture;
    }

    /// <inheritdoc/>
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member
    public override string CommandText
    {
        get => _commandText;
        set
        {
            _commandText = value;
            // Apply table prefix when setting command text
            _innerCommand.CommandText = _fixture.ApplyTablePrefixToSql(value);
        }
    }
#pragma warning restore CS8765

    /// <inheritdoc/>
    public override int CommandTimeout
    {
        get => _innerCommand.CommandTimeout;
        set => _innerCommand.CommandTimeout = value;
    }

    /// <inheritdoc/>
    public override CommandType CommandType
    {
        get => _innerCommand.CommandType;
        set => _innerCommand.CommandType = value;
    }

    /// <inheritdoc/>
    public override UpdateRowSource UpdatedRowSource
    {
        get => _innerCommand.UpdatedRowSource;
        set => _innerCommand.UpdatedRowSource = value;
    }

    /// <inheritdoc/>
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member
    protected override DbConnection? DbConnection
    {
        get => _innerCommand.Connection;
        set => _innerCommand.Connection = value!;
    }
#pragma warning restore CS8765

    /// <inheritdoc/>
    protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;

    /// <inheritdoc/>
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member
    protected override DbTransaction? DbTransaction
    {
        get => _innerCommand.Transaction;
        set => _innerCommand.Transaction = value!;
    }
#pragma warning restore CS8765

    /// <inheritdoc/>
    public override bool DesignTimeVisible
    {
        get => _innerCommand.DesignTimeVisible;
        set => _innerCommand.DesignTimeVisible = value;
    }

    /// <inheritdoc/>
    public override void Cancel() => _innerCommand.Cancel();

    /// <inheritdoc/>
    public override int ExecuteNonQuery() => _innerCommand.ExecuteNonQuery();

    /// <inheritdoc/>
    public override object? ExecuteScalar() => _innerCommand.ExecuteScalar();

    /// <inheritdoc/>
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        _innerCommand.ExecuteReader(behavior);

    /// <inheritdoc/>
    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
        _innerCommand.ExecuteNonQueryAsync(cancellationToken);

    /// <inheritdoc/>
    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) =>
        _innerCommand.ExecuteScalarAsync(cancellationToken);

    /// <inheritdoc/>
    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) =>
        _innerCommand.ExecuteReaderAsync(behavior, cancellationToken);

    /// <inheritdoc/>
    public override void Prepare() => _innerCommand.Prepare();

    /// <inheritdoc/>
    protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerCommand.Dispose();
        }

        base.Dispose(disposing);
    }
}
