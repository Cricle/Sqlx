// -----------------------------------------------------------------------
// <copyright file="SyntaxColoringTestExample.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

// This file demonstrates the SQL syntax coloring in SqlTemplate attributes
// Open this file in Visual Studio with the Sqlx extension installed to see the coloring

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.Extension.Examples
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
    }

    public interface IUserRepository
    {
        // Example 1: Simple SELECT with placeholders
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
        Task<User> GetByIdAsync(long id, CancellationToken cancellationToken = default);

        // Example 2: SELECT with multiple conditions and parameters
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND is_active = @active ORDER BY name ASC")]
        Task<List<User>> GetActiveUsersAsync(int minAge, bool active, CancellationToken cancellationToken = default);

        // Example 3: INSERT with placeholders
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        Task<int> InsertAsync(User user, CancellationToken cancellationToken = default);

        // Example 4: UPDATE with SET placeholder
        [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
        Task<int> UpdateAsync(User user, CancellationToken cancellationToken = default);

        // Example 5: DELETE with parameter
        [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
        Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default);

        // Example 6: Complex query with JOIN
        [SqlTemplate("SELECT u.{{columns}}, o.id as order_id FROM {{table}} u LEFT JOIN orders o ON u.id = o.user_id WHERE u.age > @age")]
        Task<List<User>> GetUsersWithOrdersAsync(int age, CancellationToken cancellationToken = default);

        // Example 7: Aggregate functions
        [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE is_active = @active")]
        Task<long> CountActiveUsersAsync(bool active, CancellationToken cancellationToken = default);

        // Example 8: Batch operations
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{batch_values --exclude Id}}")]
        Task<int> BatchInsertAsync(List<User> users, CancellationToken cancellationToken = default);

        // Example 9: LIMIT and OFFSET
        [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit --param limit}} {{offset --param offset}}")]
        Task<List<User>> GetPagedAsync(int limit, int offset, CancellationToken cancellationToken = default);

        // Example 10: String literals and comments
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE status = 'active' -- get only active users")]
        Task<List<User>> GetActiveUsersWithCommentAsync(CancellationToken cancellationToken = default);

        // Example 11: CASE WHEN
        [SqlTemplate("SELECT {{columns}}, CASE WHEN age >= 18 THEN 'adult' ELSE 'minor' END as category FROM {{table}}")]
        Task<List<User>> GetUsersWithCategoryAsync(CancellationToken cancellationToken = default);

        // Example 12: GROUP BY and HAVING
        [SqlTemplate("SELECT age, COUNT(*) as count FROM {{table}} GROUP BY age HAVING COUNT(*) > @minCount")]
        Task<List<object>> GetAgeStatisticsAsync(int minCount, CancellationToken cancellationToken = default);
    }
}

/*
 * Color Legend (when viewed with extension):
 * 
 * - SQL Keywords (Blue): SELECT, INSERT, UPDATE, DELETE, FROM, WHERE, JOIN, ORDER BY, GROUP BY, etc.
 * - Placeholders (Orange): {{columns}}, {{table}}, {{values}}, {{set}}, {{batch_values}}, etc.
 * - Parameters (Teal/Green): @id, @minAge, @active, @age, @limit, @offset, @minCount, etc.
 * - String Literals (Brown): 'active', 'adult', 'minor'
 * - Comments (Green): -- get only active users
 * 
 * The coloring helps you:
 * 1. Quickly identify SQL structure
 * 2. Spot errors in placeholders or parameters
 * 3. Distinguish between static and dynamic parts of queries
 * 4. Understand query intent at a glance
 */

