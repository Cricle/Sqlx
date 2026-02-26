// <copyright file="PrefixedDbConnection.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Data;
using System.Data.Common;

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// A wrapper around DbConnection that creates PrefixedDbCommand instances.
/// This ensures all commands automatically apply table prefixes for test isolation.
/// </summary>
public class PrefixedDbConnection : DbConnection
{
    private readonly DbConnection _innerConnection;
    private readonly DatabaseFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefixedDbConnection"/> class.
    /// </summary>
    /// <param name="innerConnection">The inner connection to wrap.</param>
    /// <param name="fixture">The database fixture providing the table prefix.</param>
    public PrefixedDbConnection(DbConnection innerConnection, DatabaseFixture fixture)
    {
        _innerConnection = innerConnection;
        _fixture = fixture;
    }

    /// <inheritdoc/>
    public override string ConnectionString
    {
        get => _innerConnection.ConnectionString;
        set => _innerConnection.ConnectionString = value;
    }

    /// <inheritdoc/>
    public override string Database => _innerConnection.Database;

    /// <inheritdoc/>
    public override string DataSource => _innerConnection.DataSource;

    /// <inheritdoc/>
    public override string ServerVersion => _innerConnection.ServerVersion;

    /// <inheritdoc/>
    public override ConnectionState State => _innerConnection.State;

    /// <inheritdoc/>
    public override void ChangeDatabase(string databaseName) => _innerConnection.ChangeDatabase(databaseName);

    /// <inheritdoc/>
    public override void Close() => _innerConnection.Close();

    /// <inheritdoc/>
    public override void Open() => _innerConnection.Open();

    /// <inheritdoc/>
    public override Task OpenAsync(CancellationToken cancellationToken) =>
        _innerConnection.OpenAsync(cancellationToken);

    /// <inheritdoc/>
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        _innerConnection.BeginTransaction(isolationLevel);

    /// <inheritdoc/>
    protected override DbCommand CreateDbCommand()
    {
        var innerCommand = _innerConnection.CreateCommand();
        return new PrefixedDbCommand(innerCommand, _fixture);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerConnection.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await _innerConnection.DisposeAsync();
        await base.DisposeAsync();
    }
}
