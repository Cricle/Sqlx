// <copyright file="ExceptionHandlerTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Sqlx.Tests.Core;

/// <summary>
/// Tests for ExceptionHandler covering exception enrichment, logging, retry logic, and parameter sanitization.
/// </summary>
[TestClass]
public class ExceptionHandlerTests
{
    private static readonly Type ExceptionHandlerType = typeof(SqlxException).Assembly.GetType("Sqlx.ExceptionHandler")!;
    private static readonly MethodInfo ExecuteWithHandlingAsyncMethod = ExceptionHandlerType.GetMethod(
        "ExecuteWithHandlingAsync",
        BindingFlags.Public | BindingFlags.Static)!;

    private static async Task<T> CallExecuteWithHandlingAsync<T>(
        Func<Task<T>> operation,
        SqlxContextOptions options,
        string methodName,
        string sql,
        IReadOnlyDictionary<string, object?>? parameters,
        DbTransaction? transaction)
    {
        var genericMethod = ExecuteWithHandlingAsyncMethod.MakeGenericMethod(typeof(T));
        var task = (Task<T>)genericMethod.Invoke(null, new object?[] { operation, options, methodName, sql, parameters, transaction })!;
        return await task;
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var options = new SqlxContextOptions();
        var expectedResult = 42;

        // Act
        var result = await CallExecuteWithHandlingAsync(
            () => Task.FromResult(expectedResult),
            options,
            "TestMethod",
            "SELECT * FROM Users",
            null,
            null);

        // Assert
        Assert.AreEqual(expectedResult, result);
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_OperationThrows_EnrichesException()
    {
        // Arrange
        var options = new SqlxContextOptions();
        var originalException = new InvalidOperationException("Original error");
        var sql = "SELECT * FROM Users WHERE Id = @id";
        var parameters = new Dictionary<string, object?> { ["id"] = 123 };
        var methodName = "GetUserAsync";

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                () => throw originalException,
                options,
                methodName,
                sql,
                parameters,
                null));

        Assert.IsTrue(exception.Message.Contains(methodName));
        Assert.IsTrue(exception.Message.Contains("Original error"));
        Assert.AreEqual(sql, exception.Sql);
        Assert.IsNotNull(exception.Parameters);
        Assert.AreEqual(methodName, exception.MethodName);
        Assert.IsNotNull(exception.Duration);
        Assert.AreSame(originalException, exception.InnerException);
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_WithTransaction_IncludesIsolationLevel()
    {
        // Arrange
        var options = new SqlxContextOptions();
        var mockTransaction = new TestDbTransaction(IsolationLevel.Serializable);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                () => throw new InvalidOperationException("Error"),
                options,
                "TestMethod",
                "SELECT 1",
                null,
                mockTransaction));

        Assert.AreEqual(IsolationLevel.Serializable, exception.TransactionIsolationLevel);
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_SanitizesPasswordParameter()
    {
        // Arrange
        var options = new SqlxContextOptions();
        var parameters = new Dictionary<string, object?>
        {
            ["username"] = "admin",
            ["password"] = "secret123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                () => throw new InvalidOperationException("Error"),
                options,
                "TestMethod",
                "SELECT 1",
                parameters,
                null));

        Assert.AreEqual("admin", exception.Parameters!["username"]);
        Assert.AreEqual("***REDACTED***", exception.Parameters["password"]);
    }

    [TestMethod]
    [DataRow("pwd")]
    [DataRow("secret")]
    [DataRow("token")]
    [DataRow("apikey")]
    [DataRow("api_key")]
    public async Task ExecuteWithHandlingAsync_SanitizesSensitiveParameters(string paramName)
    {
        // Arrange
        var options = new SqlxContextOptions();
        var parameters = new Dictionary<string, object?> { [paramName] = "sensitive-value" };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                () => throw new InvalidOperationException("Error"),
                options,
                "TestMethod",
                "SELECT 1",
                parameters,
                null));

        Assert.AreEqual("***REDACTED***", exception.Parameters![paramName]);
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_SanitizationIsCaseInsensitive()
    {
        // Arrange
        var options = new SqlxContextOptions();
        var parameters = new Dictionary<string, object?>
        {
            ["PASSWORD"] = "secret1",
            ["Password"] = "secret2",
            ["PaSsWoRd"] = "secret3"
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                () => throw new InvalidOperationException("Error"),
                options,
                "TestMethod",
                "SELECT 1",
                parameters,
                null));

        Assert.AreEqual("***REDACTED***", exception.Parameters!["PASSWORD"]);
        Assert.AreEqual("***REDACTED***", exception.Parameters["Password"]);
        Assert.AreEqual("***REDACTED***", exception.Parameters["PaSsWoRd"]);
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_WithNullParameters_HandlesCorrectly()
    {
        // Arrange
        var options = new SqlxContextOptions();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                () => throw new InvalidOperationException("Error"),
                options,
                "TestMethod",
                "SELECT 1",
                null,
                null));

        Assert.IsNull(exception.Parameters);
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_WithActivity_CapturesCorrelationId()
    {
        // Arrange
        var options = new SqlxContextOptions();
        var activity = new Activity("TestActivity").Start();

        try
        {
            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
                await CallExecuteWithHandlingAsync<int>(
                    () => throw new InvalidOperationException("Error"),
                    options,
                    "TestMethod",
                    "SELECT 1",
                    null,
                    null));

            Assert.IsNotNull(exception.CorrelationId);
            Assert.AreEqual(activity.Id, exception.CorrelationId);
        }
        finally
        {
            activity.Stop();
        }
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_TransientError_RetriesOperation()
    {
        // Arrange
        var options = new SqlxContextOptions
        {
            EnableRetry = true,
            MaxRetryCount = 3,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10)
        };
        var attemptCount = 0;

        // Act
        var result = await CallExecuteWithHandlingAsync(
            async () =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    throw new TimeoutException("Transient error");
                }
                return 42;
            },
            options,
            "TestMethod",
            "SELECT 1",
            null,
            null);

        // Assert
        Assert.AreEqual(42, result);
        Assert.AreEqual(3, attemptCount);
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_TransientErrorExceedsMaxRetries_ThrowsException()
    {
        // Arrange
        var options = new SqlxContextOptions
        {
            EnableRetry = true,
            MaxRetryCount = 2,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10)
        };
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                () =>
                {
                    attemptCount++;
                    throw new TimeoutException("Transient error");
                },
                options,
                "TestMethod",
                "SELECT 1",
                null,
                null));

        Assert.AreEqual(2, attemptCount);
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_NonTransientError_DoesNotRetry()
    {
        // Arrange
        var options = new SqlxContextOptions
        {
            EnableRetry = true,
            MaxRetryCount = 3,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10)
        };
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                () =>
                {
                    attemptCount++;
                    throw new InvalidOperationException("Non-transient error");
                },
                options,
                "TestMethod",
                "SELECT 1",
                null,
                null));

        Assert.AreEqual(1, attemptCount);
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_RetryDisabled_DoesNotRetry()
    {
        // Arrange
        var options = new SqlxContextOptions
        {
            EnableRetry = false,
            MaxRetryCount = 3,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10)
        };
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                () =>
                {
                    attemptCount++;
                    throw new TimeoutException("Transient error");
                },
                options,
                "TestMethod",
                "SELECT 1",
                null,
                null));

        Assert.AreEqual(1, attemptCount);
    }

    [TestMethod]
    [DataRow(-2)]
    [DataRow(1205)]
    [DataRow(1213)]
    [DataRow(40001)]
    [DataRow(40197)]
    [DataRow(40501)]
    [DataRow(40613)]
    public async Task ExecuteWithHandlingAsync_DbExceptionWithTransientErrorCode_IsTransient(int errorCode)
    {
        // Arrange
        var options = new SqlxContextOptions
        {
            EnableRetry = true,
            MaxRetryCount = 2,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10)
        };
        var attemptCount = 0;

        // Act
        var result = await CallExecuteWithHandlingAsync(
            async () =>
            {
                attemptCount++;
                if (attemptCount < 2)
                {
                    throw new TestDbException(errorCode);
                }
                return 42;
            },
            options,
            "TestMethod",
            "SELECT 1",
            null,
            null);

        // Assert
        Assert.AreEqual(42, result);
        Assert.AreEqual(2, attemptCount);
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_DbExceptionWithNonTransientErrorCode_DoesNotRetry()
    {
        // Arrange
        var options = new SqlxContextOptions
        {
            EnableRetry = true,
            MaxRetryCount = 3,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10)
        };
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                () =>
                {
                    attemptCount++;
                    throw new TestDbException(999);
                },
                options,
                "TestMethod",
                "SELECT 1",
                null,
                null));

        Assert.AreEqual(1, attemptCount);
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_WithOnExceptionCallback_InvokesCallback()
    {
        // Arrange
        SqlxException? capturedEx = null;
        var options = new SqlxContextOptions
        {
            OnException = ex =>
            {
                capturedEx = ex;
                return Task.CompletedTask;
            }
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                () => throw new InvalidOperationException("Error"),
                options,
                "TestMethod",
                "SELECT 1",
                null,
                null));

        Assert.IsNotNull(capturedEx);
        Assert.AreEqual("TestMethod", capturedEx.MethodName);
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_CallbackThrows_SwallowsCallbackException()
    {
        // Arrange
        var options = new SqlxContextOptions
        {
            OnException = ex => throw new InvalidOperationException("Callback error")
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                () => throw new InvalidOperationException("Original error"),
                options,
                "TestMethod",
                "SELECT 1",
                null,
                null));

        Assert.IsTrue(exception.InnerException!.Message.Contains("Original error"));
    }

    [TestMethod]
    public async Task ExecuteWithHandlingAsync_MeasuresDuration()
    {
        // Arrange
        var options = new SqlxContextOptions();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<SqlxException>(async () =>
            await CallExecuteWithHandlingAsync<int>(
                async () =>
                {
                    await Task.Delay(50);
                    throw new InvalidOperationException("Error");
                },
                options,
                "TestMethod",
                "SELECT 1",
                null,
                null));

        Assert.IsNotNull(exception.Duration);
        Assert.IsTrue(exception.Duration.Value.TotalMilliseconds >= 40);
    }

    [TestMethod]
    public async Task HandleExceptionAsync_WithCommandParameters_ExtractsParametersLogsAndInvokesCallback()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM users WHERE id = @id AND password = @password";
        command.Parameters.AddWithValue("@id", 123);
        command.Parameters.AddWithValue("@password", "secret123");

        SqlxException? callbackException = null;
        var logger = new TestLogger();
        var options = new SqlxContextOptions
        {
            Logger = logger,
            OnException = ex =>
            {
                callbackException = ex;
                return Task.CompletedTask;
            }
        };

        // Act
        var sqlxException = await ExceptionHandler.HandleExceptionAsync(
            new InvalidOperationException("Boom"),
            options,
            "BrokenAsync",
            command.CommandText,
            command.Parameters,
            null,
            TimeSpan.FromMilliseconds(12));

        // Assert
        Assert.AreEqual("BrokenAsync", sqlxException.MethodName);
        Assert.AreEqual(123L, Convert.ToInt64(sqlxException.Parameters!["id"]));
        Assert.AreEqual("***REDACTED***", sqlxException.Parameters["password"]);
        Assert.AreSame(sqlxException, callbackException);
        Assert.AreEqual(1, logger.Entries.Count(e => e.LogLevel == LogLevel.Error));
        Assert.AreSame(sqlxException, logger.Entries.Single(e => e.LogLevel == LogLevel.Error).Exception);
    }

    [TestMethod]
    public void HandleException_CallbackThrows_LogsWarningAndReturnsSqlxException()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT @token";
        command.Parameters.AddWithValue("@token", "abc");

        var logger = new TestLogger();
        var options = new SqlxContextOptions
        {
            Logger = logger,
            OnException = _ => throw new InvalidOperationException("Callback error")
        };

        // Act
        var sqlxException = ExceptionHandler.HandleException(
            new InvalidOperationException("Boom"),
            options,
            "BrokenSync",
            command.CommandText,
            command.Parameters,
            null,
            TimeSpan.FromMilliseconds(5));

        // Assert
        Assert.AreEqual("BrokenSync", sqlxException.MethodName);
        Assert.AreEqual("***REDACTED***", sqlxException.Parameters!["token"]);
        Assert.AreEqual(1, logger.Entries.Count(e => e.LogLevel == LogLevel.Error));
        Assert.AreEqual(1, logger.Entries.Count(e => e.LogLevel == LogLevel.Warning));
        Assert.AreEqual("Callback error", logger.Entries.Single(e => e.LogLevel == LogLevel.Warning).Exception!.Message);
    }

    private class TestDbException : DbException
    {
        public TestDbException(int errorCode)
        {
            ErrorCode = errorCode;
        }

        public override int ErrorCode { get; }
    }

    private class TestDbTransaction : DbTransaction
    {
        private readonly IsolationLevel _isolationLevel;

        public TestDbTransaction(IsolationLevel isolationLevel)
        {
            _isolationLevel = isolationLevel;
        }

        public override IsolationLevel IsolationLevel => _isolationLevel;
        protected override DbConnection? DbConnection => null;
        public override void Commit() { }
        public override void Rollback() { }
    }

    private sealed class TestLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, EventId EventId, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
