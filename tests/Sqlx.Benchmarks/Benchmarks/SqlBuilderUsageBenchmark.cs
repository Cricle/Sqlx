// -----------------------------------------------------------------------
// <copyright file="SqlBuilderUsageBenchmark.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark for common SqlBuilder usage scenarios.
/// Tests real-world patterns like SELECT, INSERT, UPDATE, DELETE with dynamic conditions.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SqlBuilderUsageBenchmark
{
    private PlaceholderContext _context = null!;
    private SqlDialect _dialect = null!;

    [GlobalSetup]
    public void Setup()
    {
        var columns = new List<ColumnMeta>
        {
            new("id", "Id", DbType.Int64, false),
            new("name", "Name", DbType.String, false),
            new("email", "Email", DbType.String, false),
            new("age", "Age", DbType.Int32, false),
            new("created_at", "CreatedAt", DbType.DateTime, false),
            new("is_active", "IsActive", DbType.Boolean, false)
        };
        _context = new PlaceholderContext(
            dialect: SqlDefine.SQLite,
            tableName: "users",
            columns: columns);
        _dialect = SqlDefine.SQLite;
    }

    // ========== Simple SELECT Query ==========

    [Benchmark(Description = "Simple SELECT with WHERE", Baseline = true)]
    public string SimpleSelect()
    {
        using var builder = new SqlBuilder(_dialect);
        var minAge = 18;
        var status = "active";
        builder.Append($"SELECT id, name, email FROM users WHERE age >= {minAge} AND status = {status}");
        return builder.Build().Sql;
    }

    [Benchmark(Description = "SELECT with multiple conditions")]
    public string SelectMultipleConditions()
    {
        using var builder = new SqlBuilder(_dialect);
        var minAge = 18;
        var maxAge = 65;
        var city = "Seattle";
        var isActive = true;
        builder.Append($"SELECT * FROM users WHERE age >= {minAge} AND age <= {maxAge} AND city = {city} AND is_active = {isActive}");
        return builder.Build().Sql;
    }

    [Benchmark(Description = "SELECT with ORDER BY and LIMIT")]
    public string SelectWithOrderAndLimit()
    {
        using var builder = new SqlBuilder(_dialect);
        var minAge = 18;
        builder.Append($"SELECT * FROM users WHERE age >= {minAge}");
        builder.AppendRaw(" ORDER BY created_at DESC LIMIT 10");
        return builder.Build().Sql;
    }

    // ========== Dynamic WHERE Conditions ==========

    [Benchmark(Description = "Dynamic WHERE (2 conditions)")]
    public string DynamicWhere2Conditions()
    {
        using var builder = new SqlBuilder(_dialect);
        builder.AppendRaw("SELECT * FROM users WHERE 1=1");
        
        var name = "John";
        var age = 30;
        if (!string.IsNullOrEmpty(name))
        {
            builder.Append($" AND name = {name}");
        }
        if (age > 0)
        {
            builder.Append($" AND age = {age}");
        }
        
        return builder.Build().Sql;
    }

    [Benchmark(Description = "Dynamic WHERE (5 conditions)")]
    public string DynamicWhere5Conditions()
    {
        using var builder = new SqlBuilder(_dialect);
        builder.AppendRaw("SELECT * FROM users WHERE 1=1");
        
        var name = "John";
        var minAge = 18;
        var maxAge = 65;
        var city = "Seattle";
        var isActive = true;
        
        if (!string.IsNullOrEmpty(name))
        {
            builder.Append($" AND name LIKE {'%' + name + '%'}");
        }
        if (minAge > 0)
        {
            builder.Append($" AND age >= {minAge}");
        }
        if (maxAge > 0)
        {
            builder.Append($" AND age <= {maxAge}");
        }
        if (!string.IsNullOrEmpty(city))
        {
            builder.Append($" AND city = {city}");
        }
        builder.Append($" AND is_active = {isActive}");
        
        return builder.Build().Sql;
    }

    // ========== INSERT Queries ==========

    [Benchmark(Description = "Simple INSERT")]
    public string SimpleInsert()
    {
        using var builder = new SqlBuilder(_dialect);
        var name = "John Doe";
        var email = "john@example.com";
        var age = 30;
        builder.Append($"INSERT INTO users (name, email, age) VALUES ({name}, {email}, {age})");
        return builder.Build().Sql;
    }

    [Benchmark(Description = "INSERT with many columns (10)")]
    public string InsertManyColumns()
    {
        using var builder = new SqlBuilder(_dialect);
        var name = "John Doe";
        var email = "john@example.com";
        var age = 30;
        var city = "Seattle";
        var country = "USA";
        var phone = "555-1234";
        var address = "123 Main St";
        var zipCode = "98101";
        var isActive = true;
        var createdAt = DateTime.UtcNow;
        
        builder.Append($"INSERT INTO users (name, email, age, city, country, phone, address, zip_code, is_active, created_at) VALUES ({name}, {email}, {age}, {city}, {country}, {phone}, {address}, {zipCode}, {isActive}, {createdAt})");
        return builder.Build().Sql;
    }

    // ========== UPDATE Queries ==========

    [Benchmark(Description = "Simple UPDATE")]
    public string SimpleUpdate()
    {
        using var builder = new SqlBuilder(_dialect);
        var name = "Jane Doe";
        var email = "jane@example.com";
        var userId = 123;
        builder.Append($"UPDATE users SET name = {name}, email = {email} WHERE id = {userId}");
        return builder.Build().Sql;
    }

    [Benchmark(Description = "UPDATE with multiple fields (5)")]
    public string UpdateMultipleFields()
    {
        using var builder = new SqlBuilder(_dialect);
        var name = "Jane Doe";
        var email = "jane@example.com";
        var age = 31;
        var city = "Portland";
        var updatedAt = DateTime.UtcNow;
        var userId = 123;
        
        builder.Append($"UPDATE users SET name = {name}, email = {email}, age = {age}, city = {city}, updated_at = {updatedAt} WHERE id = {userId}");
        return builder.Build().Sql;
    }

    // ========== DELETE Queries ==========

    [Benchmark(Description = "Simple DELETE")]
    public string SimpleDelete()
    {
        using var builder = new SqlBuilder(_dialect);
        var userId = 123;
        builder.Append($"DELETE FROM users WHERE id = {userId}");
        return builder.Build().Sql;
    }

    [Benchmark(Description = "DELETE with multiple conditions")]
    public string DeleteMultipleConditions()
    {
        using var builder = new SqlBuilder(_dialect);
        var minAge = 18;
        var isActive = false;
        var lastLoginBefore = DateTime.UtcNow.AddYears(-1);
        builder.Append($"DELETE FROM users WHERE age < {minAge} OR (is_active = {isActive} AND last_login < {lastLoginBefore})");
        return builder.Build().Sql;
    }

    // ========== Subquery Scenarios ==========

    [Benchmark(Description = "SELECT with subquery")]
    public string SelectWithSubquery()
    {
        using var subquery = new SqlBuilder(_dialect);
        var minTotal = 1000;
        subquery.Append($"SELECT user_id FROM orders WHERE total > {minTotal}");

        using var mainQuery = new SqlBuilder(_dialect);
        var minAge = 18;
        mainQuery.Append($"SELECT * FROM users WHERE age >= {minAge} AND id IN ");
        mainQuery.AppendSubquery(subquery);
        
        return mainQuery.Build().Sql;
    }

    [Benchmark(Description = "SELECT with nested subqueries")]
    public string SelectWithNestedSubqueries()
    {
        using var innerSubquery = new SqlBuilder(_dialect);
        var minAmount = 500;
        innerSubquery.Append($"SELECT order_id FROM order_items WHERE amount > {minAmount}");

        using var outerSubquery = new SqlBuilder(_dialect);
        var minTotal = 1000;
        outerSubquery.Append($"SELECT user_id FROM orders WHERE total > {minTotal} AND id IN ");
        outerSubquery.AppendSubquery(innerSubquery);

        using var mainQuery = new SqlBuilder(_dialect);
        var minAge = 18;
        mainQuery.Append($"SELECT * FROM users WHERE age >= {minAge} AND id IN ");
        mainQuery.AppendSubquery(outerSubquery);
        
        return mainQuery.Build().Sql;
    }

    // ========== Template Usage ==========

    [Benchmark(Description = "SELECT with template placeholders")]
    public string SelectWithTemplate()
    {
        using var builder = new SqlBuilder(_context);
        var minAge = 18;
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", new { minAge });
        return builder.Build().Sql;
    }

    [Benchmark(Description = "INSERT with template placeholders")]
    public string InsertWithTemplate()
    {
        using var builder = new SqlBuilder(_context);
        var name = "John Doe";
        var email = "john@example.com";
        var age = 30;
        builder.AppendTemplate("INSERT INTO {{table}} (name, email, age) VALUES (@name, @email, @age)", 
            new { name, email, age });
        return builder.Build().Sql;
    }

    // ========== Complex Real-World Scenarios ==========

    [Benchmark(Description = "Complex query: JOIN + WHERE + ORDER + LIMIT")]
    public string ComplexQueryWithJoin()
    {
        using var builder = new SqlBuilder(_dialect);
        var minAge = 18;
        var minTotal = 1000;
        var startDate = DateTime.UtcNow.AddMonths(-6);
        
        builder.Append($"SELECT u.*, COUNT(o.id) as order_count, SUM(o.total) as total_spent FROM users u");
        builder.AppendRaw(" LEFT JOIN orders o ON u.id = o.user_id");
        builder.Append($" WHERE u.age >= {minAge} AND o.total > {minTotal} AND o.created_at >= {startDate}");
        builder.AppendRaw(" GROUP BY u.id HAVING COUNT(o.id) > 5");
        builder.AppendRaw(" ORDER BY total_spent DESC LIMIT 20");
        
        return builder.Build().Sql;
    }

    [Benchmark(Description = "Pagination query")]
    public string PaginationQuery()
    {
        using var builder = new SqlBuilder(_dialect);
        var searchTerm = "John";
        var minAge = 18;
        var isActive = true;
        var pageSize = 20;
        var offset = 40; // Page 3
        
        builder.Append($"SELECT * FROM users WHERE name LIKE {'%' + searchTerm + '%'} AND age >= {minAge} AND is_active = {isActive}");
        builder.AppendRaw($" ORDER BY created_at DESC LIMIT {pageSize} OFFSET {offset}");
        
        return builder.Build().Sql;
    }

    [Benchmark(Description = "Bulk INSERT (5 rows)")]
    public string BulkInsert5Rows()
    {
        using var builder = new SqlBuilder(_dialect);
        builder.AppendRaw("INSERT INTO users (name, email, age) VALUES ");
        
        for (int i = 0; i < 5; i++)
        {
            if (i > 0) builder.AppendRaw(", ");
            var name = $"User{i}";
            var email = $"user{i}@example.com";
            var age = 20 + i;
            builder.Append($"({name}, {email}, {age})");
        }
        
        return builder.Build().Sql;
    }

    [Benchmark(Description = "UPSERT (INSERT OR UPDATE)")]
    public string UpsertQuery()
    {
        using var builder = new SqlBuilder(_dialect);
        var id = 123;
        var name = "John Doe";
        var email = "john@example.com";
        var age = 30;
        var updatedAt = DateTime.UtcNow;
        
        builder.Append($"INSERT INTO users (id, name, email, age, created_at) VALUES ({id}, {name}, {email}, {age}, {updatedAt})");
        builder.Append($" ON CONFLICT(id) DO UPDATE SET name = {name}, email = {email}, age = {age}, updated_at = {updatedAt}");
        
        return builder.Build().Sql;
    }

    // ========== Mixed AppendRaw and Append ==========

    [Benchmark(Description = "Mixed AppendRaw and Append")]
    public string MixedAppendRawAndAppend()
    {
        using var builder = new SqlBuilder(_dialect);
        var userId = 123;
        var minAge = 18;
        var status = "active";
        
        builder.AppendRaw("SELECT u.id, u.name, u.email, ");
        builder.AppendRaw("(SELECT COUNT(*) FROM orders WHERE user_id = u.id) as order_count ");
        builder.AppendRaw("FROM users u WHERE ");
        builder.Append($"u.id = {userId} AND u.age >= {minAge} AND u.status = {status}");
        
        return builder.Build().Sql;
    }
}
