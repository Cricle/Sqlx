// <copyright file="SetInlineExpressionExample.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Samples;

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

/// <summary>
/// Example demonstrating the use of {{set --inline}} for SQL expressions in UPDATE statements.
/// </summary>
public class SetInlineExpressionExample
{
    /// <summary>
    /// Entity with version tracking and timestamps.
    /// </summary>
    [SqlxEntity]
    public class Document
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int ViewCount { get; set; }
    }

    /// <summary>
    /// Repository interface demonstrating inline expression usage.
    /// </summary>
    [SqlxRepository]
    public interface IDocumentRepository
    {
        /// <summary>
        /// Updates a document and automatically increments its version.
        /// </summary>
        [SqlTemplate(@"
            UPDATE {{table}} 
            SET {{set --exclude Id,Version,UpdatedAt --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} 
            WHERE id = @id
        ")]
        Task<int> UpdateWithVersionAsync(long id, string title, string content, int viewCount);

        /// <summary>
        /// Increments the view count without updating other fields.
        /// </summary>
        [SqlTemplate(@"
            UPDATE {{table}} 
            SET {{set --exclude Id,Title,Content,Version,UpdatedAt --inline ViewCount=ViewCount+1}} 
            WHERE id = @id
        ")]
        Task<int> IncrementViewCountAsync(long id);

        /// <summary>
        /// Increments view count by a specific amount.
        /// </summary>
        [SqlTemplate(@"
            UPDATE {{table}} 
            SET {{set --exclude Id,Title,Content,Version,UpdatedAt --inline ViewCount=ViewCount+@increment}} 
            WHERE id = @id
        ")]
        Task<int> IncrementViewCountByAsync(long id, int increment);

        /// <summary>
        /// Updates document with complex expression (e.g., doubling the view count).
        /// </summary>
        [SqlTemplate(@"
            UPDATE {{table}} 
            SET {{set --exclude Id,ViewCount --inline ViewCount=ViewCount*2}} 
            WHERE id = @id
        ")]
        Task<int> DoubleViewCountAsync(long id, string title, string content, int version, DateTime updatedAt);

        /// <summary>
        /// Batch update with conditional expression.
        /// </summary>
        [SqlTemplate(@"
            UPDATE {{table}} 
            SET {{set --exclude Id --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP,ViewCount=ViewCount+@increment}} 
            WHERE id = @id
        ")]
        Task<int> BatchUpdateAsync(long id, string title, string content, int increment);
    }

    public static async Task RunExampleAsync()
    {
        Console.WriteLine("=== Set Inline Expression Examples ===\n");
        Console.WriteLine("1. Update with auto-increment version:");
        Console.WriteLine("   Template: UPDATE {{table}} SET {{set --exclude Id,Version,UpdatedAt --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id");
        Console.WriteLine("   Generated SQL (SQLite): UPDATE [documents] SET [title] = @title, [content] = @content, [view_count] = @view_count, [version] = [version] + 1, [updated_at] = CURRENT_TIMESTAMP WHERE id = @id\n");
        Console.WriteLine("2. Increment view count:");
        Console.WriteLine("   Template: UPDATE {{table}} SET {{set --exclude Id,Title,Content,Version,UpdatedAt --inline ViewCount=ViewCount+1}} WHERE id = @id");
        Console.WriteLine("   Generated SQL (SQLite): UPDATE [documents] SET [view_count] = [view_count] + 1 WHERE id = @id\n");
        Console.WriteLine("3. Increment by parameter:");
        Console.WriteLine("   Template: UPDATE {{table}} SET {{set --exclude Id,Title,Content,Version,UpdatedAt --inline ViewCount=ViewCount+@increment}} WHERE id = @id");
        Console.WriteLine("   Generated SQL (SQLite): UPDATE [documents] SET [view_count] = [view_count] + @increment WHERE id = @id\n");
        Console.WriteLine("4. Complex expression (double view count):");
        Console.WriteLine("   Template: UPDATE {{table}} SET {{set --exclude Id,ViewCount --inline ViewCount=ViewCount*2}} WHERE id = @id");
        Console.WriteLine("   Generated SQL (SQLite): UPDATE [documents] SET [title] = @title, [content] = @content, [version] = @version, [updated_at] = @updated_at, [view_count] = [view_count] * 2 WHERE id = @id\n");
        Console.WriteLine("5. Multiple expressions:");
        Console.WriteLine("   Template: UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP,ViewCount=ViewCount+@increment}} WHERE id = @id");
        Console.WriteLine("   Generated SQL (SQLite): UPDATE [documents] SET [title] = @title, [content] = @content, [version] = [version] + 1, [updated_at] = CURRENT_TIMESTAMP, [view_count] = [view_count] + @increment WHERE id = @id\n");
        Console.WriteLine("Key Features:\n- Use C# property names (PascalCase) in expressions\n- Property names are automatically converted to column names\n- Column names are wrapped with dialect-specific quotes\n- Parameter placeholders (@param) are preserved\n- Supports complex SQL expressions and functions");
    }
}
