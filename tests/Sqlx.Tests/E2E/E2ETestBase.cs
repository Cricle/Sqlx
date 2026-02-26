// <copyright file="E2ETestBase.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E;

/// <summary>
/// Base class for all E2E tests providing common infrastructure.
/// </summary>
[TestClass]
public abstract class E2ETestBase
{
    /// <summary>
    /// Gets the container manager instance.
    /// </summary>
    protected static IContainerManager ContainerManager { get; private set; } = null!;

    /// <summary>
    /// Gets the database fixture factory.
    /// </summary>
    protected static IDatabaseFixtureFactory FixtureFactory { get; private set; } = null!;

    /// <summary>
    /// Gets the data generator.
    /// </summary>
    protected static IDataGenerator DataGenerator { get; private set; } = null!;

    /// <summary>
    /// Gets the schema factory.
    /// </summary>
    protected static ISchemaFactory SchemaFactory { get; private set; } = null!;

    /// <summary>
    /// Gets or sets the test context.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Initializes the test infrastructure before any tests in this class run.
    /// </summary>
    /// <param name="context">The test context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
    public static async Task ClassInitialize(TestContext context)
    {
        ContainerManager = Infrastructure.ContainerManager.Instance;
        SchemaFactory = new SchemaFactory();
        DataGenerator = new DataGenerator();
        FixtureFactory = new DatabaseFixtureFactory(ContainerManager);

        // Check if Docker is available (for informational purposes only)
        // Containers will be started lazily when tests actually need them
        var dockerAvailable = await ContainerManager.IsDockerAvailableAsync();
        if (!dockerAvailable)
        {
            context.WriteLine("Docker is not available. Only SQLite E2E tests will run.");
            context.WriteLine("To run tests for MySQL, PostgreSQL, and SQL Server, ensure Docker Desktop is running.");
        }
        else
        {
            context.WriteLine("Docker is available. Database containers will be started on-demand when tests need them.");
        }
    }

    /// <summary>
    /// Cleans up the test infrastructure after all tests in this class complete.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ClassCleanup(InheritanceBehavior.BeforeEachDerivedClass)]
    public static async Task ClassCleanup()
    {
        if (ContainerManager != null && ContainerManager.IsInitialized)
        {
            await ContainerManager.DisposeContainersAsync();
        }
    }

    /// <summary>
    /// Creates a database fixture for the specified database type.
    /// </summary>
    /// <param name="dbType">The database type.</param>
    /// <returns>A new database fixture.</returns>
    protected async Task<IDatabaseFixture> CreateFixtureAsync(DatabaseType dbType)
    {
        if (FixtureFactory == null)
        {
            Assert.Inconclusive("Fixture factory is not initialized. Test skipped.");
        }

        // For non-SQLite databases, check if Docker is available
        // The container will be started lazily when the fixture is created
        if (dbType != DatabaseType.SQLite && !await ContainerManager.IsDockerAvailableAsync())
        {
            Assert.Inconclusive($"Docker is not available. {dbType} test skipped. Only SQLite tests can run without Docker.");
        }

        return await FixtureFactory.CreateFixtureAsync(dbType);
    }

    /// <summary>
    /// Logs a message to the test context.
    /// </summary>
    /// <param name="message">The message to log.</param>
    protected void Log(string message)
    {
        TestContext?.WriteLine(message);
    }

    /// <summary>
    /// Logs an error message to the test context.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="ex">The exception.</param>
    protected void LogError(string message, Exception ex)
    {
        TestContext?.WriteLine($"ERROR: {message}");
        TestContext?.WriteLine($"Exception: {ex.Message}");
        TestContext?.WriteLine($"Stack Trace: {ex.StackTrace}");
    }

    /// <summary>
    /// Applies table prefix to SQL statement for test isolation.
    /// </summary>
    /// <param name="fixture">The database fixture.</param>
    /// <param name="sql">The original SQL statement.</param>
    /// <returns>The SQL with prefixed table names.</returns>
    protected string ApplyTablePrefix(IDatabaseFixture fixture, string sql)
    {
        if (fixture is DatabaseFixture dbFixture)
        {
            return dbFixture.ApplyTablePrefixToSql(sql);
        }

        return sql;
    }
}
