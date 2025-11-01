// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using UnifiedDialectDemo.Models;
using UnifiedDialectDemo.Repositories;

Console.WriteLine("🎯 Sqlx Unified Dialect Demonstration");
Console.WriteLine("=====================================\n");

// ============================================
// Part 1: Demonstrate Placeholder System
// ============================================
Console.WriteLine("📋 Part 1: Placeholder System");
Console.WriteLine("------------------------------");

DemonstratePlaceholders();

Console.WriteLine();

// ============================================
// Part 2: Demonstrate Template Inheritance
// ============================================
Console.WriteLine("🔧 Part 2: Template Inheritance Resolver");
Console.WriteLine("----------------------------------------");

// Note: This would normally be done by the source generator
// We're demonstrating it manually here
DemonstrateTemplateInheritance();

Console.WriteLine();

// ============================================
// Part 3: Demonstrate Dialect Helper
// ============================================
Console.WriteLine("🛠️  Part 3: Dialect Helper");
Console.WriteLine("-------------------------");

DemonstrateDialectHelper();

Console.WriteLine();

// ============================================
// Part 4: SQLite In-Memory Demo
// ============================================
Console.WriteLine("💾 Part 4: SQLite In-Memory Demo");
Console.WriteLine("---------------------------------");

await DemonstrateSQLiteRepository();

Console.WriteLine("\n✅ Demo completed successfully!");

// ============================================
// Helper Methods
// ============================================

static void DemonstratePlaceholders()
{
    var template = "SELECT * FROM {{table}} WHERE active = {{bool_true}} AND created_at >= {{current_timestamp}}";
    
    Console.WriteLine($"Original Template:\n  {template}\n");
    Console.WriteLine("Replacements by dialect:");
    
    Console.WriteLine("\nPostgreSQL:");
    Console.WriteLine("  SELECT * FROM \"products\" WHERE active = true AND created_at >= CURRENT_TIMESTAMP");
    
    Console.WriteLine("\nMySQL:");
    Console.WriteLine("  SELECT * FROM `products` WHERE active = 1 AND created_at >= NOW()");
    
    Console.WriteLine("\nSQL Server:");
    Console.WriteLine("  SELECT * FROM [products] WHERE active = 1 AND created_at >= GETDATE()");
    
    Console.WriteLine("\nSQLite:");
    Console.WriteLine("  SELECT * FROM \"products\" WHERE active = 1 AND created_at >= datetime('now')");
}

static void DemonstrateTemplateInheritance()
{
    Console.WriteLine("Simulating template inheritance resolution...");
    Console.WriteLine("(In real usage, this is done automatically by the source generator)\n");
    
    // Simulate what the source generator would do:
    // 1. Detect IProductRepositoryBase has methods with {{placeholders}}
    // 2. For PostgreSQLProductRepository with Dialect=PostgreSQL
    // 3. Resolve all inherited templates and replace placeholders
    
    Console.WriteLine("Step 1: Interface detected → IProductRepositoryBase");
    Console.WriteLine("Step 2: Found 8 methods with SQL templates");
    Console.WriteLine("Step 3: Detected placeholders in templates");
    Console.WriteLine("Step 4: Repository class → PostgreSQLProductRepository");
    Console.WriteLine("Step 5: Extracted Dialect → PostgreSQL");
    Console.WriteLine("Step 6: Extracted TableName → products");
    Console.WriteLine("Step 7: Replacing placeholders...");
    
    Console.WriteLine("\nExamples for PostgreSQL:");
    Console.WriteLine("\n  GetByIdAsync:");
    Console.WriteLine("    Before: SELECT * FROM {{table}} WHERE id = @id");
    Console.WriteLine("    After:  SELECT * FROM \"products\" WHERE id = @id");
    
    Console.WriteLine("\n  InsertAsync:");
    Console.WriteLine("    Before: INSERT INTO {{table}} (...) VALUES (...) {{returning_id}}");
    Console.WriteLine("    After:  INSERT INTO \"products\" (...) VALUES (...) RETURNING id");
    
    Console.WriteLine("\n  DeactivateAsync:");
    Console.WriteLine("    Before: UPDATE {{table}} SET is_active = {{bool_false}} WHERE id = @id");
    Console.WriteLine("    After:  UPDATE \"products\" SET is_active = false WHERE id = @id");
}

static void DemonstrateDialectHelper()
{
    Console.WriteLine("Dialect extraction examples:\n");
    
    // Simulate what DialectHelper would extract from attributes
    Console.WriteLine("From PostgreSQLProductRepository:");
    Console.WriteLine("  - Dialect: PostgreSQL");
    Console.WriteLine("  - TableName: products (from RepositoryFor attribute)");
    Console.WriteLine("  - Should use template inheritance: Yes\n");
    
    Console.WriteLine("From SQLiteProductRepository:");
    Console.WriteLine("  - Dialect: SQLite");
    Console.WriteLine("  - TableName: products (from RepositoryFor attribute)");
    Console.WriteLine("  - Should use template inheritance: Yes");
    
    Console.WriteLine("\nDialect Provider Factory:");
    Console.WriteLine("  PostgreSql → PostgreSqlDialectProvider");
    Console.WriteLine("  MySql → MySqlDialectProvider");
    Console.WriteLine("  SqlServer → SqlServerDialectProvider");
    Console.WriteLine("  SQLite → SQLiteDialectProvider");
}

static async Task DemonstrateSQLiteRepository()
{
    Console.WriteLine("Creating in-memory SQLite database...\n");
    
    using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    
    // Create table
    using (var cmd = connection.CreateCommand())
    {
        cmd.CommandText = @"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                description TEXT,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TEXT
            )";
        await cmd.ExecuteNonQueryAsync();
    }
    
    Console.WriteLine("✅ Table created");
    
    // Note: In real usage, SQLiteProductRepository would be generated
    // For this demo, we'll just show what SQL would be generated
    
    Console.WriteLine("\nGenerated SQL for SQLiteProductRepository would be:");
    Console.WriteLine("\n1. GetActiveProductsAsync():");
    Console.WriteLine("   SELECT * FROM \"products\" WHERE is_active = 1 ORDER BY name");
    
    Console.WriteLine("\n2. InsertAsync():");
    Console.WriteLine("   INSERT INTO \"products\" (name, description, price, stock, is_active, created_at)");
    Console.WriteLine("   VALUES (@name, @description, @price, @stock, 1, datetime('now'))");
    Console.WriteLine("   -- Then uses: SELECT last_insert_rowid()");
    
    Console.WriteLine("\n3. DeactivateAsync():");
    Console.WriteLine("   UPDATE \"products\"");
    Console.WriteLine("   SET is_active = 0, updated_at = datetime('now')");
    Console.WriteLine("   WHERE id = @id");
    
    // Demonstrate manual query execution (what the generated code would do)
    Console.WriteLine("\nExecuting sample insert...");
    using (var cmd = connection.CreateCommand())
    {
        cmd.CommandText = @"
            INSERT INTO products (name, description, price, stock, is_active, created_at)
            VALUES (@name, @description, @price, @stock, 1, datetime('now'))";
        cmd.Parameters.AddWithValue("@name", "Demo Product");
        cmd.Parameters.AddWithValue("@description", "This is a demo");
        cmd.Parameters.AddWithValue("@price", 99.99);
        cmd.Parameters.AddWithValue("@stock", 100);
        
        await cmd.ExecuteNonQueryAsync();
    }
    
    long lastId;
    using (var cmd = connection.CreateCommand())
    {
        cmd.CommandText = "SELECT last_insert_rowid()";
        lastId = (long)(await cmd.ExecuteScalarAsync() ?? 0);
    }
    
    Console.WriteLine($"✅ Product inserted with ID: {lastId}");
    
    // Query it back
    Console.WriteLine("\nQuerying product...");
    using (var cmd = connection.CreateCommand())
    {
        cmd.CommandText = @"SELECT * FROM ""products"" WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", lastId);
        
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            Console.WriteLine($"  ID: {reader["id"]}");
            Console.WriteLine($"  Name: {reader["name"]}");
            Console.WriteLine($"  Price: {reader["price"]}");
            Console.WriteLine($"  Stock: {reader["stock"]}");
            Console.WriteLine($"  Active: {reader["is_active"]}");
            Console.WriteLine($"  Created: {reader["created_at"]}");
        }
    }
}

