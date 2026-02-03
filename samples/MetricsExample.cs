// <copyright file="MetricsExample.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Diagnostics;

namespace Sqlx.Samples;

/// <summary>
/// Demonstrates SQL template metrics collection for performance monitoring.
/// </summary>
public class MetricsExample
{
    [Sqlx]
    public class Product
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    [SqlxRepository]
    public interface IProductRepository : ICrudRepository<Product, long> { }

    [TableName("products")]
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IProductRepository))]
    public partial class ProductRepository(SqliteConnection connection) : IProductRepository { }

    public static async Task Main()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL, stock INTEGER)";
            await cmd.ExecuteNonQueryAsync();
        }
        var repo = new ProductRepository(connection);
        Console.WriteLine("=== SQL Template Metrics Demo ===\n");
        for (int i = 0; i < 100; i++)
        {
            await repo.InsertAsync(new Product { Name = $"Product {i}", Price = 10.0m + i, Stock = 100 - i });
        }
        for (int i = 0; i < 50; i++)
        {
            await repo.GetByIdAsync(i + 1);
        }
        var products = await repo.GetAllAsync(limit: 20);
        await repo.CountAsync();
        try { await repo.GetByIdAsync(999999); } catch { }
        Console.WriteLine(MetricsFormatter.FormatGlobalMetrics(SqlTemplateMetrics.GetGlobalMetrics()));
        Console.WriteLine("\n=== Top 5 Most Executed Templates ===");
        Console.WriteLine(MetricsFormatter.FormatMetricsTable(SqlTemplateMetrics.GetMostExecutedTemplates(5)));
        Console.WriteLine("\n=== Detailed Metrics for InsertAsync ===");
        var insertMetrics = SqlTemplateMetrics.GetMetrics("Sqlx.Samples.ProductRepository.InsertAsync");
        if (insertMetrics.HasValue)
            Console.WriteLine(MetricsFormatter.FormatTemplateMetrics(insertMetrics.Value));
        Console.WriteLine("\n=== JSON Export ===");
        Console.WriteLine(MetricsFormatter.FormatAsJson(SqlTemplateMetrics.GetGlobalMetrics()));
    }
}
