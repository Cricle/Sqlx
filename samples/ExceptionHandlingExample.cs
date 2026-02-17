// Exception Handling 使用示例
// 展示如何使用 SqlxContextOptions 配置异常处理、重试和日志记录

using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqlx;

namespace Sqlx.Samples;

/// <summary>
/// 异常处理示例程序
/// </summary>
public class ExceptionHandlingExample
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Sqlx 异常处理示例 ===\n");

        // 示例 1: 基本异常处理
        await Example1_BasicExceptionHandling();

        // 示例 2: 启用重试机制
        await Example2_RetryMechanism();

        // 示例 3: 自定义异常回调
        await Example3_CustomExceptionCallback();

        // 示例 4: 集成日志记录
        await Example4_LoggingIntegration();

        // 示例 5: 依赖注入配置
        await Example5_DependencyInjection();

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }

    /// <summary>
    /// 示例 1: 基本异常处理 - SqlxException 包含丰富的上下文信息
    /// </summary>
    static async Task Example1_BasicExceptionHandling()
    {
        Console.WriteLine("示例 1: 基本异常处理");
        Console.WriteLine("-------------------");

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // 创建 SqlxContext（不配置任何选项）
        var options = new SqlxContextOptions();
        await using var context = new TestContext(connection, options);

        try
        {
            // 尝试查询不存在的表
            await context.TestRepo.ExecuteAsync("SELECT * FROM non_existent_table");
        }
        catch (SqlxException ex)
        {
            Console.WriteLine($"捕获到 SqlxException:");
            Console.WriteLine($"  方法: {ex.MethodName}");
            Console.WriteLine($"  SQL: {ex.Sql}");
            Console.WriteLine($"  执行时长: {ex.Duration?.TotalMilliseconds}ms");
            Console.WriteLine($"  关联ID: {ex.CorrelationId}");
            Console.WriteLine($"  错误: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 示例 2: 启用重试机制 - 自动重试瞬态错误
    /// </summary>
    static async Task Example2_RetryMechanism()
    {
        Console.WriteLine("示例 2: 启用重试机制");
        Console.WriteLine("-------------------");

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // 配置重试选项
        var options = new SqlxContextOptions
        {
            EnableRetry = true,
            MaxRetryCount = 3,
            InitialRetryDelay = TimeSpan.FromMilliseconds(100),
            RetryBackoffMultiplier = 2.0
        };

        await using var context = new TestContext(connection, options);

        Console.WriteLine("配置:");
        Console.WriteLine($"  启用重试: {options.EnableRetry}");
        Console.WriteLine($"  最大重试次数: {options.MaxRetryCount}");
        Console.WriteLine($"  初始延迟: {options.InitialRetryDelay.TotalMilliseconds}ms");
        Console.WriteLine($"  退避倍数: {options.RetryBackoffMultiplier}");
        Console.WriteLine();
        Console.WriteLine("注意: 重试仅对瞬态错误生效（超时、死锁等）");
        Console.WriteLine();
    }

    /// <summary>
    /// 示例 3: 自定义异常回调 - 记录遥测数据或发送通知
    /// </summary>
    static async Task Example3_CustomExceptionCallback()
    {
        Console.WriteLine("示例 3: 自定义异常回调");
        Console.WriteLine("---------------------");

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // 配置异常回调
        var options = new SqlxContextOptions
        {
            OnException = async (ex) =>
            {
                // 记录到遥测系统
                Console.WriteLine($"[遥测] 记录异常:");
                Console.WriteLine($"  方法: {ex.MethodName}");
                Console.WriteLine($"  SQL: {ex.Sql}");
                Console.WriteLine($"  时长: {ex.Duration?.TotalMilliseconds}ms");
                
                // 模拟异步操作（如发送到监控系统）
                await Task.Delay(10);
                
                // 可以在这里发送通知、记录到外部系统等
                // await telemetryClient.TrackException(ex);
                // await notificationService.NotifyAdmins(ex);
            }
        };

        await using var context = new TestContext(connection, options);

        try
        {
            await context.TestRepo.ExecuteAsync("SELECT * FROM non_existent_table");
        }
        catch (SqlxException)
        {
            Console.WriteLine("\n异常回调已执行，然后异常被抛出");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 示例 4: 集成日志记录 - 自动记录异常和重试
    /// </summary>
    static async Task Example4_LoggingIntegration()
    {
        Console.WriteLine("示例 4: 集成日志记录");
        Console.WriteLine("-------------------");

        // 创建日志工厂
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Warning);
        });

        var logger = loggerFactory.CreateLogger<TestContext>();

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // 配置日志记录
        var options = new SqlxContextOptions
        {
            Logger = logger,
            EnableRetry = true,
            MaxRetryCount = 2
        };

        await using var context = new TestContext(connection, options);

        Console.WriteLine("配置了 ILogger，异常和重试将自动记录到日志");
        Console.WriteLine();

        try
        {
            await context.TestRepo.ExecuteAsync("SELECT * FROM non_existent_table");
        }
        catch (SqlxException)
        {
            Console.WriteLine("\n异常已记录到日志并抛出");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 示例 5: 依赖注入配置 - 在 ASP.NET Core 中使用
    /// </summary>
    static async Task Example5_DependencyInjection()
    {
        Console.WriteLine("示例 5: 依赖注入配置");
        Console.WriteLine("-------------------");

        // 创建服务集合
        var services = new ServiceCollection();

        // 配置日志
        services.AddLogging(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information);
        });

        // 注册数据库连接
        services.AddSingleton<SqliteConnection>(sp =>
        {
            var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            return conn;
        });

        // 注册 SqlxContext 并配置选项
        services.AddSqlxContext<TestContext>((sp, options) =>
        {
            var connection = sp.GetRequiredService<SqliteConnection>();
            var logger = sp.GetRequiredService<ILogger<TestContext>>();

            // 配置异常处理选项
            options.Logger = logger;
            options.EnableRetry = true;
            options.MaxRetryCount = 3;
            options.InitialRetryDelay = TimeSpan.FromMilliseconds(100);
            options.OnException = async (ex) =>
            {
                // 自定义异常处理逻辑
                Console.WriteLine($"[DI] 异常回调: {ex.MethodName}");
                await Task.CompletedTask;
            };

            return new TestContext(connection, options, sp);
        }, ServiceLifetime.Scoped);

        // 构建服务提供者
        await using var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("SqlxContext 已通过 DI 注册，配置了:");
        Console.WriteLine("  - 日志记录");
        Console.WriteLine("  - 重试机制");
        Console.WriteLine("  - 自定义异常回调");
        Console.WriteLine();

        // 使用服务
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();

        Console.WriteLine("可以从 DI 容器中解析 SqlxContext 并使用");
        Console.WriteLine();
    }

    // 测试用的 SqlxContext 实现
    private class TestContext : SqlxContext
    {
        public TestRepository TestRepo { get; }

        public TestContext(
            SqliteConnection connection,
            SqlxContextOptions? options = null,
            IServiceProvider? serviceProvider = null)
            : base(connection, options)
        {
            TestRepo = new TestRepository();
            TestRepo.Connection = connection;
            TestRepo.Transaction = Transaction;
        }

        protected override void PropagateTransactionToRepositories()
        {
            TestRepo.Transaction = Transaction;
        }

        protected override void ClearRepositoryTransactions()
        {
            TestRepo.Transaction = null;
        }
    }

    // 测试用的 Repository
    private class TestRepository : ISqlxRepository
    {
        public IDbConnection? Connection { get; set; }
        public IDbTransaction? Transaction { get; set; }

        public async Task<int> ExecuteAsync(string sql)
        {
            if (Connection == null)
                throw new InvalidOperationException("Connection is not set");

            using var command = Connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = Transaction;

            if (command is SqliteCommand sqliteCommand)
            {
                return await sqliteCommand.ExecuteNonQueryAsync();
            }

            return command.ExecuteNonQuery();
        }
    }
}
