using System.Data.Common;
using FullDemo.Database;
using FullDemo.Repositories;
using FullDemo.Repositories.SQLite;
using FullDemo.Repositories.PostgreSQL;
using FullDemo.Repositories.MySQL;
using FullDemo.Repositories.SqlServer;
using FullDemo.Tests;
using Microsoft.Data.Sqlite;
using Npgsql;
using MySqlConnector;
using Microsoft.Data.SqlClient;

namespace FullDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           Sqlx Full Demo - All Database Features               ║");
        Console.WriteLine("║                                                                ║");
        Console.WriteLine("║  Supported Databases:                                          ║");
        Console.WriteLine("║    • SQLite (built-in)                                         ║");
        Console.WriteLine("║    • PostgreSQL                                                ║");
        Console.WriteLine("║    • MySQL                                                     ║");
        Console.WriteLine("║    • SQL Server (not included in this demo)                    ║");
        Console.WriteLine("║    • Oracle (not included in this demo)                        ║");
        Console.WriteLine("║    • DB2 (not included in this demo)                           ║");
        Console.WriteLine("║                                                                ║");
        Console.WriteLine("║  Features Demonstrated:                                        ║");
        Console.WriteLine("║    • ICrudRepository (50+ methods)                             ║");
        Console.WriteLine("║    • Expression-based queries                                  ║");
        Console.WriteLine("║    • Soft Delete                                               ║");
        Console.WriteLine("║    • Audit Fields                                              ║");
        Console.WriteLine("║    • Optimistic Locking (Version)                              ║");
        Console.WriteLine("║    • Batch Operations                                          ║");
        Console.WriteLine("║    • Aggregate Functions (Count, Sum, Avg, Max, Min)           ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // 解析命令行参数
        var dbType = args.Length > 0 ? args[0].ToLower() : "sqlite";

        Console.WriteLine($"Running tests with database: {dbType.ToUpper()}");
        Console.WriteLine(new string('=', 60));

        try
        {
            switch (dbType)
            {
                case "sqlite":
                    await RunSQLiteTestsAsync();
                    break;

                case "postgresql":
                case "postgres":
                case "pg":
                    await RunPostgreSQLTestsAsync(args);
                    break;

                case "mysql":
                    await RunMySQLTestsAsync(args);
                    break;

                case "sqlserver":
                case "mssql":
                    await RunSqlServerTestsAsync(args);
                    break;

                case "all":
                    await RunSQLiteTestsAsync();
                    await RunPostgreSQLTestsAsync(args);
                    await RunMySQLTestsAsync(args);
                    await RunSqlServerTestsAsync(args);
                    break;

                default:
                    Console.WriteLine($"Unknown database type: {dbType}");
                    Console.WriteLine("Usage: FullDemo [sqlite|postgresql|mysql|all] [connection-args...]");
                    return;
            }

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("✓ All tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    static async Task RunSQLiteTestsAsync()
    {
        Console.WriteLine("\n┌──────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│                    SQLite Database Tests                      │");
        Console.WriteLine("└──────────────────────────────────────────────────────────────┘");

        // 删除旧数据库文件
        if (File.Exists("fulldemo.db"))
        {
            File.Delete("fulldemo.db");
        }

        using var connection = SQLiteInitializer.CreateConnection();
        await connection.OpenAsync();
        await SQLiteInitializer.InitializeAsync(connection);

        // 创建仓储
        var userRepo = new SQLiteUserRepository(connection);
        var productRepo = new SQLiteProductRepository(connection);
        var orderRepo = new SQLiteOrderRepository(connection);
        var accountRepo = new SQLiteAccountRepository(connection);
        var logRepo = new SQLiteLogRepository(connection);

        // 运行测试
        await new UserRepositoryTests(userRepo, "SQLite").RunAllTestsAsync();
        await new ProductRepositoryTests(productRepo, "SQLite").RunAllTestsAsync();
        await new OrderRepositoryTests(orderRepo, userRepo, "SQLite").RunAllTestsAsync();
        await new AccountRepositoryTests(accountRepo, "SQLite").RunAllTestsAsync();
        await new LogRepositoryTests(logRepo, "SQLite").RunAllTestsAsync();
    }

    static async Task RunPostgreSQLTestsAsync(string[] args)
    {
        Console.WriteLine("\n┌──────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│                   PostgreSQL Database Tests                   │");
        Console.WriteLine("└──────────────────────────────────────────────────────────────┘");

        // 从参数或环境变量获取连接信息
        var host = GetArg(args, "--pg-host") ?? Environment.GetEnvironmentVariable("PG_HOST") ?? "localhost";
        var port = int.Parse(GetArg(args, "--pg-port") ?? Environment.GetEnvironmentVariable("PG_PORT") ?? "5432");
        var database = GetArg(args, "--pg-database") ?? Environment.GetEnvironmentVariable("PG_DATABASE") ?? "fulldemo";
        var user = GetArg(args, "--pg-user") ?? Environment.GetEnvironmentVariable("PG_USER") ?? "postgres";
        var password = GetArg(args, "--pg-password") ?? Environment.GetEnvironmentVariable("PG_PASSWORD") ?? "postgres";

        Console.WriteLine($"Connecting to PostgreSQL at {host}:{port}/{database}...");

        // 创建数据库（如果不存在）
        try
        {
            var masterConn = $"Host={host};Port={port};Database=postgres;Username={user};Password={password}";
            using (var conn = new NpgsqlConnection(masterConn))
            {
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{database}'";
                var exists = await cmd.ExecuteScalarAsync();
                if (exists == null)
                {
                    cmd.CommandText = $"CREATE DATABASE {database}";
                    await cmd.ExecuteNonQueryAsync();
                    Console.WriteLine($"Created database: {database}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not check/create database: {ex.Message}");
        }

        using var connection = PostgreSQLInitializer.CreateConnection(host, port, database, user, password);
        await connection.OpenAsync();
        await PostgreSQLInitializer.InitializeAsync(connection);

        // 创建仓储
        var userRepo = new PostgreSQLUserRepository(connection);
        var productRepo = new PostgreSQLProductRepository(connection);
        var orderRepo = new PostgreSQLOrderRepository(connection);
        var accountRepo = new PostgreSQLAccountRepository(connection);
        var logRepo = new PostgreSQLLogRepository(connection);

        // 运行测试
        await new UserRepositoryTests(userRepo, "PostgreSQL").RunAllTestsAsync();
        await new ProductRepositoryTests(productRepo, "PostgreSQL").RunAllTestsAsync();
        await new OrderRepositoryTests(orderRepo, userRepo, "PostgreSQL").RunAllTestsAsync();
        await new AccountRepositoryTests(accountRepo, "PostgreSQL").RunAllTestsAsync();
        await new LogRepositoryTests(logRepo, "PostgreSQL").RunAllTestsAsync();
    }

    static async Task RunMySQLTestsAsync(string[] args)
    {
        Console.WriteLine("\n┌──────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│                     MySQL Database Tests                      │");
        Console.WriteLine("└──────────────────────────────────────────────────────────────┘");

        // 从参数或环境变量获取连接信息
        var host = GetArg(args, "--mysql-host") ?? Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
        var port = int.Parse(GetArg(args, "--mysql-port") ?? Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306");
        var database = GetArg(args, "--mysql-database") ?? Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "fulldemo";
        var user = GetArg(args, "--mysql-user") ?? Environment.GetEnvironmentVariable("MYSQL_USER") ?? "root";
        var password = GetArg(args, "--mysql-password") ?? Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? "root";

        Console.WriteLine($"Connecting to MySQL at {host}:{port}/{database}...");

        // 创建数据库（如果不存在）
        await MySQLInitializer.CreateDatabaseIfNotExistsAsync(host, port, database, user, password);

        using var connection = MySQLInitializer.CreateConnection(host, port, database, user, password);
        await connection.OpenAsync();
        await MySQLInitializer.InitializeAsync(connection);

        // 创建仓储
        var userRepo = new MySQLUserRepository(connection);
        var productRepo = new MySQLProductRepository(connection);
        var orderRepo = new MySQLOrderRepository(connection);
        var accountRepo = new MySQLAccountRepository(connection);
        var logRepo = new MySQLLogRepository(connection);

        // 运行测试
        await new UserRepositoryTests(userRepo, "MySQL").RunAllTestsAsync();
        await new ProductRepositoryTests(productRepo, "MySQL").RunAllTestsAsync();
        await new OrderRepositoryTests(orderRepo, userRepo, "MySQL").RunAllTestsAsync();
        await new AccountRepositoryTests(accountRepo, "MySQL").RunAllTestsAsync();
        await new LogRepositoryTests(logRepo, "MySQL").RunAllTestsAsync();
    }

    static async Task RunSqlServerTestsAsync(string[] args)
    {
        Console.WriteLine("\n┌" + new string('─', 62) + "┐");
        Console.WriteLine("│" + "                  SQL Server Database Tests".PadRight(62) + "│");
        Console.WriteLine("└" + new string('─', 62) + "┘");

        // 从参数或环境变量获取连接信息
        var host = GetArg(args, "--sqlserver-host") ?? Environment.GetEnvironmentVariable("SQLSERVER_HOST") ?? "localhost";
        var port = int.Parse(GetArg(args, "--sqlserver-port") ?? Environment.GetEnvironmentVariable("SQLSERVER_PORT") ?? "1433");
        var database = GetArg(args, "--sqlserver-database") ?? Environment.GetEnvironmentVariable("SQLSERVER_DATABASE") ?? "fulldemo";
        var user = GetArg(args, "--sqlserver-user") ?? Environment.GetEnvironmentVariable("SQLSERVER_USER") ?? "sa";
        var password = GetArg(args, "--sqlserver-password") ?? Environment.GetEnvironmentVariable("SQLSERVER_PASSWORD") ?? "YourStrong@Passw0rd";

        Console.WriteLine($"Connecting to SQL Server at {host}:{port}/{database}...");

        // 创建数据库（如果不存在）
        await SqlServerInitializer.CreateDatabaseIfNotExistsAsync(host, port, database, user, password);

        using var connection = SqlServerInitializer.CreateConnection(host, port, database, user, password);
        await connection.OpenAsync();
        await SqlServerInitializer.InitializeAsync(connection);

        // 创建仓储
        var userRepo = new SqlServerUserRepository(connection);
        var productRepo = new SqlServerProductRepository(connection);
        var orderRepo = new SqlServerOrderRepository(connection);
        var accountRepo = new SqlServerAccountRepository(connection);
        var logRepo = new SqlServerLogRepository(connection);

        // 运行测试
        await new UserRepositoryTests(userRepo, "SqlServer").RunAllTestsAsync();
        await new ProductRepositoryTests(productRepo, "SqlServer").RunAllTestsAsync();
        await new OrderRepositoryTests(orderRepo, userRepo, "SqlServer").RunAllTestsAsync();
        await new AccountRepositoryTests(accountRepo, "SqlServer").RunAllTestsAsync();
        await new LogRepositoryTests(logRepo, "SqlServer").RunAllTestsAsync();
    }

    static string? GetArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }
        return null;
    }
}
