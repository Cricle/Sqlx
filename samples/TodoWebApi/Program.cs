using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using TodoWebApi.Services;
using TodoWebApi.Json;
using TodoWebApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 配置服务 - 简化配置
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, TodoJsonContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var sqliteConnection = new SqliteConnection("Data Source=todos.db;Cache=Shared;Foreign Keys=true");
builder.Services.AddSingleton(_ => sqliteConnection);

builder.Services.AddSingleton<ITodoRepository, TodoRepository>();
builder.Services.AddSingleton<DatabaseService>();

var app = builder.Build();

// 中间件配置
app.UseStaticFiles();
app.UseCors();

// API路由定义
app.MapGet("/", () => Results.Redirect("/index.html"));

app.MapGet("/api/info", () =>
    Results.Json(new ApiInfoResponse(
        "Sqlx Todo WebAPI",
        "3.0.0-aot",
        app.Environment.EnvironmentName,
        "SQLite",
        new Dictionary<string, object> { ["todos"] = "/api/todos" },
        DateTime.UtcNow), TodoJsonContext.Default.ApiInfoResponse));

app.MapGet("/api/todos", async (ITodoRepository repo) =>
    Results.Json(await repo.GetAllAsync(limit: 100), TodoJsonContext.Default.ListTodo));

app.MapGet("/api/todos/{id:long}", async (long id, ITodoRepository repo) =>
{
    var todo = await repo.GetByIdAsync(id);
    return todo is not null
        ? Results.Json(todo, TodoJsonContext.Default.Todo)
        : Results.Json(new ErrorResponse("TODO未找到"), TodoJsonContext.Default.ErrorResponse, statusCode: 404);
});

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
    
    // 获取最后插入的ID
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT last_insert_rowid()";
    var newId = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
    
    var created = todo with { Id = newId };
    return Results.Json(created, TodoJsonContext.Default.Todo, statusCode: 201);
});

app.MapPut("/api/todos/{id:long}", async (long id, UpdateTodoRequest request, ITodoRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing is null)
        return Results.Json(new ErrorResponse("TODO未找到"), TodoJsonContext.Default.ErrorResponse, statusCode: 404);

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

app.MapDelete("/api/todos/{id:long}", async (long id, ITodoRepository repo) =>
{
    var result = await repo.DeleteAsync(id);
    return result == 0
        ? Results.Json(new ErrorResponse("TODO未找到"), TodoJsonContext.Default.ErrorResponse, statusCode: 404)
        : Results.NoContent();
});

app.MapGet("/api/todos/search", async (string q, ITodoRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(q))
        return Results.Json(new ErrorResponse("搜索关键词不能为空"), TodoJsonContext.Default.ErrorResponse, statusCode: 400);

    return Results.Json(await repo.SearchAsync($"%{q.Trim()}%"), TodoJsonContext.Default.ListTodo);
});

// 新增的高级功能API端点 - 展示更多查询功能
app.MapGet("/api/todos/completed", async (ITodoRepository repo) =>
    Results.Json(await repo.GetByCompletionStatusAsync(true), TodoJsonContext.Default.ListTodo));

app.MapGet("/api/todos/high-priority", async (ITodoRepository repo) =>
    Results.Json(await repo.GetByPriorityAsync(minPriority: 3, isCompleted: false), TodoJsonContext.Default.ListTodo));

app.MapGet("/api/todos/due-soon", async (ITodoRepository repo) =>
    Results.Json(await repo.GetDueSoonAsync(DateTime.UtcNow.AddDays(7), isCompleted: false), TodoJsonContext.Default.ListTodo));

app.MapGet("/api/todos/count", async (ITodoRepository repo) =>
{
    var count = await repo.CountAsync();
    return Results.Json(new Dictionary<string, object> { ["count"] = count }, TodoJsonContext.Default.DictionaryStringObject);
});

app.MapPut("/api/todos/batch/priority", async (BatchPriorityUpdateRequest request, ITodoRepository repo) =>
{
    if (request.Ids?.Count == 0)
        return Results.Json(new ErrorResponse("ID列表不能为空"), TodoJsonContext.Default.ErrorResponse, statusCode: 400);

    // 转换为JSON数组格式供SQLite的json_each使用
    var idsJson = JsonSerializer.Serialize(request.Ids, TodoJsonContext.Default.ListInt64);
    var updated = await repo.BatchUpdatePriorityAsync(idsJson, request.NewPriority, DateTime.UtcNow);
    return Results.Json(new Dictionary<string, object> { ["updated"] = updated }, TodoJsonContext.Default.DictionaryStringObject);
});

// 数据库初始化
using var scope = app.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<DatabaseService>().InitializeDatabaseAsync();

Console.WriteLine("✅ 启动成功: http://localhost:5000");
app.Run();
