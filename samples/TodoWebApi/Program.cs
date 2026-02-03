using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Sqlx;
using TodoWebApi.Services;
using TodoWebApi.Json;
using TodoWebApi.Models;

var builder = WebApplication.CreateSlimBuilder(args);

// Configure JSON serialization for AOT
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, TodoJsonContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Configure SQLite connection
var sqliteConnection = new SqliteConnection("Data Source=todos.db;Cache=Shared;Foreign Keys=true");
sqliteConnection.Open();  // Open connection immediately for singleton
builder.Services.AddSingleton(_ => sqliteConnection);

// Register Sqlx-generated repository
builder.Services.AddSingleton<ITodoRepository, TodoRepository>();
builder.Services.AddSingleton<DatabaseService>();

var app = builder.Build();

// Configure middleware
app.UseStaticFiles();

// Redirect root to index.html
app.MapGet("/", () => Results.Redirect("/index.html"));

// Get all todos
app.MapGet("/api/todos", async (ITodoRepository repo) =>
    Results.Json(await repo.GetAllAsync(limit: 100), TodoJsonContext.Default.ListTodo));

// Get todo by ID
app.MapGet("/api/todos/{id:long}", async (long id, ITodoRepository repo) =>
{
    var todo = await repo.GetByIdAsync(id);
    return todo is not null
        ? Results.Json(todo, TodoJsonContext.Default.Todo)
        : Results.NotFound();
});

// Create todo
app.MapPost("/api/todos", async (CreateTodoRequest request, ITodoRepository repo, SqliteConnection conn) =>
{
    var todo = new Todo
    {
        Title = request.Title,
        Description = request.Description,
        IsCompleted = request.IsCompleted,
        Priority = request.Priority,
        DueDate = request.DueDate,
        Tags = request.Tags,
        EstimatedMinutes = request.EstimatedMinutes,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    var newId = await repo.InsertAndGetIdAsync(todo);
    todo.Id = newId;
    return Results.Json(todo, TodoJsonContext.Default.Todo, statusCode: 201);
});

// Update todo
app.MapPut("/api/todos/{id:long}", async (long id, UpdateTodoRequest request, ITodoRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing is null)
        return Results.NotFound();

    existing.Title = request.Title;
    existing.Description = request.Description;
    existing.IsCompleted = request.IsCompleted;
    existing.Priority = request.Priority;
    existing.DueDate = request.DueDate;
    existing.Tags = request.Tags;
    existing.EstimatedMinutes = request.EstimatedMinutes;
    existing.UpdatedAt = DateTime.UtcNow;
    existing.CompletedAt = request.IsCompleted ? (existing.CompletedAt ?? DateTime.UtcNow) : null;

    await repo.UpdateAsync(existing);
    return Results.Json(existing, TodoJsonContext.Default.Todo);
});

// Delete todo
app.MapDelete("/api/todos/{id:long}", async (long id, ITodoRepository repo) =>
{
    var result = await repo.DeleteAsync(id);
    return result == 0 ? Results.NotFound() : Results.NoContent();
});

// Mark as completed
app.MapPut("/api/todos/{id:long}/complete", async (long id, ITodoRepository repo) =>
{
    var result = await repo.MarkAsCompletedAsync(id, DateTime.UtcNow, DateTime.UtcNow);
    return result == 0 ? Results.NotFound() : Results.NoContent();
});

// Batch update priority
app.MapPut("/api/todos/batch/priority", async (BatchPriorityUpdateRequest request, ITodoRepository repo) =>
{
    var result = await repo.BatchUpdatePriorityAsync(request.Ids, request.NewPriority, DateTime.UtcNow);
    return Results.Json(new BatchUpdateResult(result), TodoJsonContext.Default.BatchUpdateResult);
});

// Search todos
app.MapGet("/api/todos/search", async (string q, ITodoRepository repo) =>
    Results.Json(await repo.SearchAsync($"%{q}%"), TodoJsonContext.Default.ListTodo));

// Get completed todos
app.MapGet("/api/todos/completed", async (ITodoRepository repo) =>
    Results.Json(await repo.GetByCompletionStatusAsync(true), TodoJsonContext.Default.ListTodo));

// Get high priority todos
app.MapGet("/api/todos/high-priority", async (ITodoRepository repo) =>
    Results.Json(await repo.GetByPriorityAsync(3, false), TodoJsonContext.Default.ListTodo));

// Get todos due soon (next 7 days)
app.MapGet("/api/todos/due-soon", async (ITodoRepository repo) =>
    Results.Json(await repo.GetDueSoonAsync(DateTime.UtcNow.AddDays(7), false), TodoJsonContext.Default.ListTodo));

// Get total count
app.MapGet("/api/todos/count", async (ITodoRepository repo) =>
    Results.Json((int)await repo.CountAsync(), TodoJsonContext.Default.Int32));

// Get pending todos count
app.MapGet("/api/todos/count/pending", async (ITodoRepository repo) =>
{
    var count = await repo.CountWhereAsync(t => !t.IsCompleted);
    return Results.Json((int)count, TodoJsonContext.Default.Int32);
});

// Get overdue todos
app.MapGet("/api/todos/overdue", async (ITodoRepository repo) =>
{
    var todos = await repo.GetWhereAsync(t => 
        t.DueDate != null && 
        t.DueDate < DateTime.UtcNow && 
        !t.IsCompleted);
    return Results.Json(todos, TodoJsonContext.Default.ListTodo);
});

// Get todos by priority
app.MapGet("/api/todos/priority/{priority:int}", async (int priority, ITodoRepository repo) =>
{
    var todos = await repo.GetWhereAsync(t => t.Priority == priority);
    return Results.Json(todos, TodoJsonContext.Default.ListTodo);
});

// Batch delete todos
app.MapDelete("/api/todos/batch", async (BatchDeleteRequest request, ITodoRepository repo) =>
{
    if (request.Ids == null || request.Ids.Count == 0)
        return Results.Json(new BatchUpdateResult(0), TodoJsonContext.Default.BatchUpdateResult);
    
    // Workaround: Delete todos one by one (for small lists this is acceptable)
    int deletedCount = 0;
    foreach (var id in request.Ids)
    {
        var result = await repo.DeleteAsync(id);
        deletedCount += result;
    }
    return Results.Json(new BatchUpdateResult(deletedCount), TodoJsonContext.Default.BatchUpdateResult);
});

// Batch complete todos
app.MapPut("/api/todos/batch/complete", async (BatchCompleteRequest request, ITodoRepository repo) =>
{
    var now = DateTime.UtcNow;
    var result = await repo.BatchCompleteAsync(request.Ids, now, now);
    return Results.Json(new BatchUpdateResult(result), TodoJsonContext.Default.BatchUpdateResult);
});

// Update actual minutes
app.MapPut("/api/todos/{id:long}/actual-minutes", async (long id, UpdateActualMinutesRequest request, ITodoRepository repo) =>
{
    var result = await repo.UpdateActualMinutesAsync(id, request.ActualMinutes, DateTime.UtcNow);
    return result == 0 ? Results.NotFound() : Results.NoContent();
});

// Get todos with pagination
app.MapGet("/api/todos/paged", async (int page, int pageSize, ITodoRepository repo) =>
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;
    
    var todos = await repo.GetPagedAsync(pageSize, (page - 1) * pageSize);
    return Results.Json(todos, TodoJsonContext.Default.ListTodo);
});

// Check if todo exists
app.MapGet("/api/todos/{id:long}/exists", async (long id, ITodoRepository repo) =>
{
    var exists = await repo.ExistsByIdAsync(id);
    return Results.Json(new ExistsResult(exists), TodoJsonContext.Default.ExistsResult);
});

// Get todos by IDs - using individual queries as workaround
app.MapPost("/api/todos/by-ids", async (BatchGetRequest request, ITodoRepository repo) =>
{
    if (request.Ids == null || request.Ids.Count == 0)
        return Results.Json(new List<Todo>(), TodoJsonContext.Default.ListTodo);
    
    // Workaround: Get todos one by one (for small lists this is acceptable)
    var todos = new List<Todo>();
    foreach (var id in request.Ids)
    {
        var todo = await repo.GetByIdAsync(id);
        if (todo != null)
            todos.Add(todo);
    }
    return Results.Json(todos, TodoJsonContext.Default.ListTodo);
});

// Delete all completed todos
app.MapDelete("/api/todos/completed", async (ITodoRepository repo) =>
{
    var result = await repo.DeleteWhereAsync(t => t.IsCompleted);
    return Results.Json(new BatchUpdateResult(result), TodoJsonContext.Default.BatchUpdateResult);
});

// ========== LINQ Expression Examples ==========

// Get todos using LINQ expression predicate
app.MapGet("/api/todos/linq/high-priority-pending", async (ITodoRepository repo) =>
{
    var todos = await repo.GetWhereAsync(t => t.Priority >= 3 && !t.IsCompleted);
    return Results.Json(todos, TodoJsonContext.Default.ListTodo);
});

// Count todos using LINQ expression
app.MapGet("/api/todos/linq/count-overdue", async (ITodoRepository repo) =>
{
    var count = await repo.CountWhereAsync(t => 
        t.DueDate != null && 
        t.DueDate < DateTime.UtcNow && 
        !t.IsCompleted);
    return Results.Json((int)count, TodoJsonContext.Default.Int32);
});

// ========== IQueryable Examples ==========

// Complex query with IQueryable - high priority todos with pagination
app.MapGet("/api/todos/queryable/priority-paged", async (ITodoRepository repo, int page = 1, int pageSize = 10) =>
{
    // Use predefined repository methods instead of IQueryable to avoid ambiguity
    var todos = await repo.GetPagedWhereAsync(t => t.Priority >= 3 && !t.IsCompleted, pageSize, (page - 1) * pageSize);
    return Results.Json(todos, TodoJsonContext.Default.ListTodo);
});

// IQueryable with projection - get only title and priority
app.MapGet("/api/todos/queryable/titles", async (ITodoRepository repo) =>
{
    // Use predefined repository methods
    var todos = await repo.GetWhereAsync(t => !t.IsCompleted);
    var result = todos
        .OrderBy(t => t.Priority)
        .Select(t => new TodoTitlePriority(t.Id, t.Title, t.Priority))
        .ToList();
    
    return Results.Json(result, TodoJsonContext.Default.ListTodoTitlePriority);
});

// IQueryable with string functions
app.MapGet("/api/todos/queryable/search-advanced", async (string keyword, ITodoRepository repo) =>
{
    // Use custom search method
    var todos = await repo.SearchAsync($"%{keyword}%");
    var result = todos.Take(20).ToList();
    return Results.Json(result, TodoJsonContext.Default.ListTodo);
});

// IQueryable with aggregation
app.MapGet("/api/todos/queryable/stats", async (ITodoRepository repo) =>
{
    var total = await repo.CountAsync();
    var completed = await repo.CountWhereAsync(t => t.IsCompleted);
    var highPriority = await repo.CountWhereAsync(t => t.Priority >= 3 && !t.IsCompleted);
    
    var stats = new TodoStatsResult(
        Total: total,
        Completed: completed,
        Pending: total - completed,
        HighPriority: highPriority,
        CompletionRate: total > 0 ? (double)completed / total * 100 : 0
    );
    
    return Results.Json(stats, TodoJsonContext.Default.TodoStatsResult);
});

// Initialize database
using var scope = app.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<DatabaseService>().InitializeDatabaseAsync();

Console.WriteLine("✅ Sqlx TODO Demo running at http://localhost:5000");
app.Run();
