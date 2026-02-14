// -----------------------------------------------------------------------
// <copyright file="TodoDbContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using Sqlx;
using Sqlx.Annotations;

namespace TodoWebApi.Services;

/// <summary>
/// Database context for the Todo application.
/// Demonstrates SqlxContext usage with lazy repository resolution via IServiceProvider.
/// </summary>
/// <remarks>
/// <para><strong>SqlxContext Features:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Lazy Repository Resolution</strong> - Repositories are resolved on first access from IServiceProvider</description></item>
/// <item><description><strong>Unified Transaction Management</strong> - All repositories participate in the same transaction</description></item>
/// <item><description><strong>Zero Reflection</strong> - All code is generated at compile time</description></item>
/// <item><description><strong>AOT Compatible</strong> - Fully supports Native AOT compilation</description></item>
/// </list>
/// 
/// <para><strong>Generated Code:</strong></para>
/// <para>The source generator automatically creates:</para>
/// <code>
/// // Store IServiceProvider for lazy resolution
/// private readonly System.IServiceProvider _serviceProvider;
/// 
/// // Auto-generated constructor
/// public TodoDbContext(DbConnection connection, IServiceProvider serviceProvider) 
///     : base(connection, ownsConnection: false)
/// {
///     _serviceProvider = serviceProvider;
/// }
/// 
/// // Lazy-resolved repository property (resolved on first access)
/// private TodoRepository? _todos;
/// public TodoRepository Todos
/// {
///     get
///     {
///         if (_todos == null)
///         {
///             _todos = _serviceProvider.GetRequiredService&lt;TodoRepository&gt;();
///             ((ISqlxRepository)_todos).Connection = Connection;
///             _todos.Transaction = Transaction;
///         }
///         return _todos;
///     }
/// }
/// 
/// // Transaction propagation
/// protected override void PropagateTransactionToRepositories()
/// {
///     if (_todos != null) _todos.Transaction = Transaction;
/// }
/// 
/// // Transaction cleanup
/// protected override void ClearRepositoryTransactions()
/// {
///     if (_todos != null) _todos.Transaction = null;
/// }
/// </code>
/// 
/// <para><strong>Usage Example:</strong></para>
/// <code>
/// // Register in DI
/// services.AddSingleton&lt;TodoRepository&gt;();
/// services.AddSqlxContext&lt;TodoDbContext&gt;(ServiceLifetime.Singleton);
/// 
/// // Use in endpoints
/// app.MapPost("/api/todos/bulk", async (List&lt;CreateTodoRequest&gt; requests, TodoDbContext context) =&gt;
/// {
///     await using var transaction = await context.BeginTransactionAsync();
///     try
///     {
///         foreach (var request in requests)
///         {
///             var todo = new Todo { /* ... */ };
///             // All operations automatically participate in the transaction
///             await context.Todos.InsertAndGetIdAsync(todo, default);
///         }
///         await transaction.CommitAsync();
///         return Results.Ok();
///     }
///     catch
///     {
///         // Transaction automatically rolled back on dispose
///         throw;
///     }
/// });
/// </code>
/// 
/// <para><strong>Benefits:</strong></para>
/// <list type="bullet">
/// <item><description>Simpler DI registration - no manual repository injection</description></item>
/// <item><description>Lazy loading - repositories only created when first accessed</description></item>
/// <item><description>Performance - unused repositories are never instantiated</description></item>
/// <item><description>Automatic configuration - Connection and Transaction set automatically</description></item>
/// <item><description>Type-safe - direct property access with IntelliSense</description></item>
/// </list>
/// 
/// <para>For more details, see the <see href="https://github.com/yourusername/Sqlx/blob/main/docs/sqlx-context.md">SqlxContext documentation</see>.</para>
/// </remarks>
[SqlxContext]
[SqlDefine(SqlDefineTypes.SQLite)]
[IncludeRepository(typeof(TodoRepository))]
public partial class TodoDbContext : SqlxContext
{
    // ===== AUTO-GENERATED CODE =====
    // The source generator creates the following members:
    //
    // 1. Service Provider Field:
    //    private readonly System.IServiceProvider _serviceProvider;
    //
    // 2. Constructor:
    //    public TodoDbContext(DbConnection connection, IServiceProvider serviceProvider)
    //        : base(connection, ownsConnection: false)
    //    { _serviceProvider = serviceProvider; }
    //
    // 3. Repository Property (lazy-resolved on first access):
    //    private TodoRepository? _todos;
    //    public TodoRepository Todos { get { if (_todos == null) { ... } return _todos; } }
    //
    // 4. Transaction Management:
    //    protected override void PropagateTransactionToRepositories() { ... }
    //    protected override void ClearRepositoryTransactions() { ... }
    //
    // See generated file: obj/Debug/net10.0/generated/Sqlx.Generator/TodoDbContext.Context.g.cs
}
