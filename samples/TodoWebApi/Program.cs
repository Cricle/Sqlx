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
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, TodoJsonContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
var sqliteConnection = new SqliteConnection("Data Source=todos.db;Cache=Shared;Foreign Keys=true");
sqliteConnection.Open();
builder.Services.AddSingleton(_ => sqliteConnection);
builder.Services.AddSingleton<ITodoRepository, TodoRepository>();
builder.Services.AddSingleton<DatabaseService>();
var app = builder.Build();

app.UseStaticFiles();
app.MapGet("/", () => Results.Redirect("/index.html"));
app.MapGet("/api/todos", async (ITodoRepository repo) => Results.Json(await repo.GetAllAsync(limit: 100), TodoJsonContext.Default.ListTodo));
app.MapGet("/api/todos/{id:long}", async (long id, ITodoRepository repo) =>
{
    var todo = await repo.GetByIdAsync(id);
    return todo is not null ? Results.Json(todo, TodoJsonContext.Default.Todo) : Results.NotFound();
});

app.MapPost("/api/todos", async (CreateTodoRequest request, ITodoRepository repo, SqliteConnection conn) =>
{
    var todo = new Todo { Title = request.Title, Description = request.Description, IsCompleted = request.IsCompleted, Priority = request.Priority, DueDate = request.DueDate, Tags = request.Tags, EstimatedMinutes = request.EstimatedMinutes, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
    var newId = await repo.InsertAndGetIdAsync(todo);
    todo.Id = newId;
    return Results.Json(todo, TodoJsonContext.Default.Todo, statusCode: 201);
});

app.MapPut("/api/todos/{id:long}", async (long id, UpdateTodoRequest request, ITodoRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing is null) return Results.NotFound();
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
app.MapDelete("/api/todos/{id:long}", async (long id, ITodoRepository repo) => await repo.DeleteAsync(id) == 0 ? Results.NotFound() : Results.NoContent());
app.MapPut("/api/todos/{id:long}/complete", async (long id, ITodoRepository repo) => await repo.DynamicUpdateAsync(id, t => new Todo { IsCompleted = true, CompletedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }) == 0 ? Results.NotFound() : Results.NoContent());

app.MapPut("/api/todos/batch/priority", async (BatchPriorityUpdateRequest request, ITodoRepository repo) =>
{
    if (request.Ids == null || request.Ids.Count == 0) return Results.Json(new BatchUpdateResult(0), TodoJsonContext.Default.BatchUpdateResult);
    var result = await repo.DynamicUpdateWhereAsync(t => new Todo { Priority = request.NewPriority, UpdatedAt = DateTime.UtcNow }, t => request.Ids.Contains(t.Id));
    return Results.Json(new BatchUpdateResult(result), TodoJsonContext.Default.BatchUpdateResult);
});
app.MapGet("/api/todos/search", async (string q, ITodoRepository repo) =>
{
    var todos = await repo.GetWhereAsync(t => t.Title.Contains(q) || (t.Description != null && t.Description.Contains(q)));
    return Results.Json(todos, TodoJsonContext.Default.ListTodo);
});
app.MapGet("/api/todos/completed", async (ITodoRepository repo) => Results.Json(await repo.GetWhereAsync(t => t.IsCompleted), TodoJsonContext.Default.ListTodo));
app.MapGet("/api/todos/high-priority", async (ITodoRepository repo) => Results.Json(await repo.GetWhereAsync(t => t.Priority >= 3 && !t.IsCompleted), TodoJsonContext.Default.ListTodo));

app.MapGet("/api/todos/due-soon", async (ITodoRepository repo) =>
{
    var dueDate = DateTime.UtcNow.AddDays(7);
    var todos = await repo.GetWhereAsync(t => t.DueDate != null && t.DueDate <= dueDate && !t.IsCompleted);
    return Results.Json(todos, TodoJsonContext.Default.ListTodo);
});
app.MapGet("/api/todos/count", async (ITodoRepository repo) => Results.Json((int)await repo.CountAsync(), TodoJsonContext.Default.Int32));
app.MapGet("/api/todos/count/pending", async (ITodoRepository repo) => Results.Json((int)await repo.CountWhereAsync(t => !t.IsCompleted), TodoJsonContext.Default.Int32));
app.MapGet("/api/todos/overdue", async (ITodoRepository repo) =>
{
    var todos = await repo.GetWhereAsync(t => t.DueDate != null && t.DueDate < DateTime.UtcNow && !t.IsCompleted);
    return Results.Json(todos, TodoJsonContext.Default.ListTodo);
});
app.MapGet("/api/todos/priority/{priority:int}", async (int priority, ITodoRepository repo) => Results.Json(await repo.GetWhereAsync(t => t.Priority == priority), TodoJsonContext.Default.ListTodo));

app.MapDelete("/api/todos/batch", async (BatchDeleteRequest request, ITodoRepository repo) =>
{
    if (request.Ids == null || request.Ids.Count == 0) return Results.Json(new BatchUpdateResult(0), TodoJsonContext.Default.BatchUpdateResult);
    var result = await repo.DeleteByIdsAsync(request.Ids);
    return Results.Json(new BatchUpdateResult(result), TodoJsonContext.Default.BatchUpdateResult);
});
app.MapPut("/api/todos/batch/complete", async (BatchCompleteRequest request, ITodoRepository repo) =>
{
    if (request.Ids == null || request.Ids.Count == 0) return Results.Json(new BatchUpdateResult(0), TodoJsonContext.Default.BatchUpdateResult);
    var now = DateTime.UtcNow;
    var result = await repo.DynamicUpdateWhereAsync(t => new Todo { IsCompleted = true, CompletedAt = now, UpdatedAt = now }, t => request.Ids.Contains(t.Id));
    return Results.Json(new BatchUpdateResult(result), TodoJsonContext.Default.BatchUpdateResult);
});
app.MapPut("/api/todos/{id:long}/actual-minutes", async (long id, UpdateActualMinutesRequest request, ITodoRepository repo) => await repo.DynamicUpdateAsync(id, t => new Todo { ActualMinutes = request.ActualMinutes, UpdatedAt = DateTime.UtcNow }) == 0 ? Results.NotFound() : Results.NoContent());

app.MapGet("/api/todos/paged", async (int page, int pageSize, ITodoRepository repo) =>
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;
    var todos = await repo.GetPagedAsync(pageSize, (page - 1) * pageSize);
    return Results.Json(todos, TodoJsonContext.Default.ListTodo);
});
app.MapGet("/api/todos/{id:long}/exists", async (long id, ITodoRepository repo) => Results.Json(new ExistsResult(await repo.ExistsByIdAsync(id)), TodoJsonContext.Default.ExistsResult));
app.MapPost("/api/todos/by-ids", async (BatchGetRequest request, ITodoRepository repo) =>
{
    if (request.Ids == null || request.Ids.Count == 0) return Results.Json(new List<Todo>(), TodoJsonContext.Default.ListTodo);
    var todos = await repo.GetByIdsAsync(request.Ids);
    return Results.Json(todos, TodoJsonContext.Default.ListTodo);
});
app.MapDelete("/api/todos/completed", async (ITodoRepository repo) => Results.Json(new BatchUpdateResult(await repo.DeleteWhereAsync(t => t.IsCompleted)), TodoJsonContext.Default.BatchUpdateResult));

app.MapGet("/api/todos/linq/high-priority-pending", async (ITodoRepository repo) => Results.Json(await repo.GetWhereAsync(t => t.Priority >= 3 && !t.IsCompleted), TodoJsonContext.Default.ListTodo));
app.MapGet("/api/todos/linq/count-overdue", async (ITodoRepository repo) => Results.Json((int)await repo.CountWhereAsync(t => t.DueDate != null && t.DueDate < DateTime.UtcNow && !t.IsCompleted), TodoJsonContext.Default.Int32));
app.MapGet("/api/todos/queryable/priority-paged", async (ITodoRepository repo, int page = 1, int pageSize = 10) => Results.Json(await repo.GetPagedWhereAsync(t => t.Priority >= 3 && !t.IsCompleted, pageSize, (page - 1) * pageSize), TodoJsonContext.Default.ListTodo));
app.MapGet("/api/todos/queryable/titles", async (ITodoRepository repo) =>
{
    var todos = await repo.GetWhereAsync(t => !t.IsCompleted);
    var result = todos.OrderByDescending(t => t.Priority).Select(t => new TodoTitlePriority(t.Id, t.Title, t.Priority)).ToList();
    return Results.Json(result, TodoJsonContext.Default.ListTodoTitlePriority);
});
app.MapGet("/api/todos/queryable/search-advanced", async (string keyword, ITodoRepository repo) => Results.Json(await repo.GetWhereAsync(t => t.Title.Contains(keyword) || (t.Description != null && t.Description.Contains(keyword)), limit: 20), TodoJsonContext.Default.ListTodo));
app.MapGet("/api/todos/queryable/stats", async (ITodoRepository repo) =>
{
    var total = await repo.CountAsync();
    var completed = await repo.CountWhereAsync(t => t.IsCompleted);
    var highPriority = await repo.CountWhereAsync(t => t.Priority >= 3 && !t.IsCompleted);
    var stats = new TodoStatsResult(Total: total, Completed: completed, Pending: total - completed, HighPriority: highPriority, CompletionRate: total > 0 ? (double)completed / total * 100 : 0);
    return Results.Json(stats, TodoJsonContext.Default.TodoStatsResult);
});
using var scope = app.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<DatabaseService>().InitializeDatabaseAsync();
Console.WriteLine("✅ Sqlx TODO Demo running at http://localhost:5000");
app.Run();
