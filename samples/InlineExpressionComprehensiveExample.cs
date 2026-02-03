// <copyright file="InlineExpressionComprehensiveExample.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Samples;

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

/// <summary>
/// Comprehensive example demonstrating inline expressions for {{set}} and {{values}} placeholders.
/// </summary>
public class InlineExpressionComprehensiveExample
{
    /// <summary>
    /// User entity with audit fields.
    /// </summary>
    [SqlxEntity]
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public string Role { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Document entity with version control.
    /// </summary>
    [SqlxEntity]
    public class Document
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Version { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Order item with computed total.
    /// </summary>
    [SqlxEntity]
    public class OrderItem
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Repository demonstrating {{set --inline}} usage.
    /// </summary>
    [SqlxRepository]
    public interface IUserRepository
    {
        // ===== INSERT with {{values --inline}} =====

        /// <summary>
        /// Creates a new user with auto-generated timestamps and default values.
        /// </summary>
        [SqlTemplate(@"
            INSERT INTO {{table}} ({{columns --exclude Id}}) 
            VALUES ({{values --exclude Id --inline Email=LOWER(@email),IsActive=1,IsVerified=0,Role='user',Version=1,CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP}})
        ")]
        Task<int> CreateUserAsync(string username, string email, string passwordHash);

        /// <summary>
        /// Creates a user with custom role.
        /// </summary>
        [SqlTemplate(@"
            INSERT INTO {{table}} ({{columns --exclude Id}}) 
            VALUES ({{values --exclude Id --inline Email=LOWER(TRIM(@email)),CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP}})
        ")]
        Task<int> CreateUserWithRoleAsync(string username, string email, string passwordHash, bool isActive, bool isVerified, string role, int version);

        // ===== UPDATE with {{set --inline}} =====

        /// <summary>
        /// Updates user and auto-increments version.
        /// </summary>
        [SqlTemplate(@"
            UPDATE {{table}} 
            SET {{set --exclude Id,Version,UpdatedAt --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} 
            WHERE id = @id AND version = @version
        ")]
        Task<int> UpdateUserWithVersionAsync(long id, string username, string email, string passwordHash, bool isActive, bool isVerified, string role, int version, DateTime createdAt);

        /// <summary>
        /// Activates a user account.
        /// </summary>
        [SqlTemplate(@"
            UPDATE {{table}} 
            SET {{set --exclude Id --inline IsActive=1,IsVerified=1,UpdatedAt=CURRENT_TIMESTAMP}} 
            WHERE id = @id
        ")]
        Task<int> ActivateUserAsync(long id, string username, string email, string passwordHash, string role, int version, DateTime createdAt);

        /// <summary>
        /// Deactivates a user account.
        /// </summary>
        [SqlTemplate(@"
            UPDATE {{table}} 
            SET {{set --exclude Id --inline IsActive=0,UpdatedAt=CURRENT_TIMESTAMP}} 
            WHERE id = @id
        ")]
        Task<int> DeactivateUserAsync(long id, string username, string email, string passwordHash, bool isVerified, string role, int version, DateTime createdAt);
    }

    /// <summary>
    /// Repository demonstrating version control with inline expressions.
    /// </summary>
    [SqlxRepository]
    public interface IDocumentRepository
    {
        /// <summary>
        /// Creates a new document with initial version.
        /// </summary>
        [SqlTemplate(@"
            INSERT INTO {{table}} ({{columns --exclude Id}}) 
            VALUES ({{values --exclude Id --inline Version=1,ViewCount=0,CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP}})
        ")]
        Task<int> CreateDocumentAsync(string title, string content);

        /// <summary>
        /// Updates document and increments version.
        /// </summary>
        [SqlTemplate(@"
            UPDATE {{table}} 
            SET {{set --exclude Id,Version,ViewCount,UpdatedAt --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} 
            WHERE id = @id AND version = @version
        ")]
        Task<int> UpdateDocumentAsync(long id, string title, string content, int version, int viewCount, DateTime createdAt);

        /// <summary>
        /// Increments view count without updating version.
        /// </summary>
        [SqlTemplate(@"
            UPDATE {{table}} 
            SET {{set --exclude Id,Title,Content,Version,CreatedAt,UpdatedAt --inline ViewCount=ViewCount+1}} 
            WHERE id = @id
        ")]
        Task<int> IncrementViewCountAsync(long id);

        /// <summary>
        /// Increments view count by a specific amount.
        /// </summary>
        [SqlTemplate(@"
            UPDATE {{table}} 
            SET {{set --exclude Id,Title,Content,Version,CreatedAt,UpdatedAt --inline ViewCount=ViewCount+@increment}} 
            WHERE id = @id
        ")]
        Task<int> IncrementViewCountByAsync(long id, int increment);
    }

    /// <summary>
    /// Repository demonstrating computed values with inline expressions.
    /// </summary>
    [SqlxRepository]
    public interface IOrderItemRepository
    {
        /// <summary>
        /// Creates an order item with computed total.
        /// </summary>
        [SqlTemplate(@"
            INSERT INTO {{table}} ({{columns --exclude Id}}) 
            VALUES ({{values --exclude Id --inline Total=@quantity*@unitPrice,CreatedAt=CURRENT_TIMESTAMP}})
        ")]
        Task<int> CreateOrderItemAsync(long orderId, string productName, int quantity, decimal unitPrice);

        /// <summary>
        /// Updates order item and recalculates total.
        /// </summary>
        [SqlTemplate(@"
            UPDATE {{table}} 
            SET {{set --exclude Id,CreatedAt --inline Total=@quantity*@unitPrice}} 
            WHERE id = @id
        ")]
        Task<int> UpdateOrderItemAsync(long id, long orderId, string productName, int quantity, decimal unitPrice, DateTime createdAt);
    }

    public static void PrintExamples()
    {
        Console.WriteLine("=== Inline Expression Examples ===\n");
        Console.WriteLine("1. INSERT with default values and timestamps:");
        Console.WriteLine("   Template: INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline IsActive=1,CreatedAt=CURRENT_TIMESTAMP}})");
        Console.WriteLine("   Generated: INSERT INTO [users] ([username], [email], ..., [is_active], [created_at]) VALUES (@username, @email, ..., 1, CURRENT_TIMESTAMP)\n");
        Console.WriteLine("2. UPDATE with version increment:");
        Console.WriteLine("   Template: UPDATE {{table}} SET {{set --exclude Id,Version --inline Version=Version+1}} WHERE id = @id");
        Console.WriteLine("   Generated: UPDATE [documents] SET [title] = @title, [content] = @content, [version] = [version] + 1 WHERE id = @id\n");
        Console.WriteLine("3. INSERT with computed value:");
        Console.WriteLine("   Template: INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Total=@quantity*@unitPrice}})");
        Console.WriteLine("   Generated: INSERT INTO [order_items] ([id], [quantity], [unit_price], [total]) VALUES (@id, @quantity, @unit_price, @quantity*@unitPrice)\n");
        Console.WriteLine("4. UPDATE with counter increment:");
        Console.WriteLine("   Template: UPDATE {{table}} SET {{set --inline ViewCount=ViewCount+1}} WHERE id = @id");
        Console.WriteLine("   Generated: UPDATE [documents] SET [view_count] = [view_count] + 1 WHERE id = @id\n");
        Console.WriteLine("5. INSERT with string transformation:");
        Console.WriteLine("   Template: INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Email=LOWER(TRIM(@email))}})");
        Console.WriteLine("   Generated: INSERT INTO [users] ([id], [email]) VALUES (@id, LOWER(TRIM(@email)))\n");
        Console.WriteLine("Key Benefits:\n- Reduce boilerplate code for common patterns\n- Ensure consistency in timestamp generation\n- Automatic version control without manual tracking\n- Computed values calculated at database level\n- Type-safe with compile-time validation");
    }
}
