using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using TodoWebApi.Services;
using TodoWebApi.Json;
using TodoWebApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization for AOT
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, TodoJsonContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Configure CORS
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

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
app.UseCors();

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

    await repo.InsertAsync(todo);

    // Get last inserted ID
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT last_insert_rowid()";
    var newId = (long)(await cmd.ExecuteScalarAsync() ?? 0L);

    var created = todo with { Id = newId };
    return Results.Json(created, TodoJsonContext.Default.Todo, statusCode: 201);
});

// Update todo
app.MapPut("/api/todos/{id:long}", async (long id, UpdateTodoRequest request, ITodoRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing is null)
        return Results.NotFound();

    var updated = existing with
    {
        Title = request.Title,
        Description = request.Description,
        IsCompleted = request.IsCompleted,
        Priority = request.Priority,
        DueDate = request.DueDate,
        Tags = request.Tags,
        EstimatedMinutes = request.EstimatedMinutes,
        UpdatedAt = DateTime.UtcNow,
        CompletedAt = request.IsCompleted ? (existing.CompletedAt ?? DateTime.UtcNow) : null
    };

    await repo.UpdateAsync(updated);
    return Results.Json(updated, TodoJsonContext.Default.Todo);
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
    // Manual JSON array construction for AOT compatibility
    var idsJson = $"[{string.Join(",", request.Ids)}]";
    var result = await repo.BatchUpdatePriorityAsync(idsJson, request.NewPriority, DateTime.UtcNow);
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
    Results.Json(await repo.CountAsync(), TodoJsonContext.Default.Int32));

// Initialize database
using var scope = app.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<DatabaseService>().InitializeDatabaseAsync();

Console.WriteLine("✅ Sqlx TODO Demo running at http://localhost:5000");
app.Run();
