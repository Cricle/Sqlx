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

builder.Services.AddSingleton<ITodoService, TodoService>();
builder.Services.AddSingleton<DatabaseService>();

// 配置 Sqlx 全局拦截器
// 1. Activity追踪（兼容OpenTelemetry）
Sqlx.Interceptors.SqlxInterceptors.Add(new Sqlx.Interceptors.ActivityInterceptor());

// 2. 简单日志拦截器（示例）
Sqlx.Interceptors.SqlxInterceptors.Add(new TodoWebApi.Interceptors.SimpleLogInterceptor());

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

app.MapGet("/api/todos", async (ITodoService service) =>
    Results.Json(await service.GetAllAsync(), TodoJsonContext.Default.ListTodo));

app.MapGet("/api/todos/{id:long}", async (long id, ITodoService service) =>
{
    var todo = await service.GetByIdAsync(id);
    return todo is not null
        ? Results.Json(todo, TodoJsonContext.Default.Todo)
        : Results.Json(new ErrorResponse("TODO未找到"), TodoJsonContext.Default.ErrorResponse, statusCode: 404);
});

app.MapPost("/api/todos", async (CreateTodoRequest request, ITodoService service) =>
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

    var id = await service.CreateAsync(todo);
    var created = await service.GetByIdAsync(id);
    return Results.Json(created, TodoJsonContext.Default.Todo, statusCode: 201);
});

app.MapPut("/api/todos/{id:long}", async (long id, UpdateTodoRequest request, ITodoService service) =>
{
    var existing = await service.GetByIdAsync(id);
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

    await service.UpdateAsync(updated);
    return Results.Json(updated, TodoJsonContext.Default.Todo);
});

app.MapDelete("/api/todos/{id:long}", async (long id, ITodoService service) =>
{
    var result = await service.DeleteAsync(id);
    return result == 0
        ? Results.Json(new ErrorResponse("TODO未找到"), TodoJsonContext.Default.ErrorResponse, statusCode: 404)
        : Results.NoContent();
});

app.MapGet("/api/todos/search", async (string q, ITodoService service) =>
{
    if (string.IsNullOrWhiteSpace(q))
        return Results.Json(new ErrorResponse("搜索关键词不能为空"), TodoJsonContext.Default.ErrorResponse, statusCode: 400);

    return Results.Json(await service.SearchAsync(q.Trim()), TodoJsonContext.Default.ListTodo);
});

// 新增的高级功能API端点 - 展示更多查询功能
app.MapGet("/api/todos/completed", async (ITodoService service) =>
    Results.Json(await service.GetCompletedAsync(true), TodoJsonContext.Default.ListTodo));

app.MapGet("/api/todos/high-priority", async (ITodoService service) =>
    Results.Json(await service.GetHighPriorityAsync(minPriority: 3, isCompleted: false), TodoJsonContext.Default.ListTodo));

app.MapGet("/api/todos/due-soon", async (ITodoService service) =>
    Results.Json(await service.GetDueSoonAsync(DateTime.UtcNow.AddDays(7), isCompleted: false), TodoJsonContext.Default.ListTodo));

app.MapGet("/api/todos/count", async (ITodoService service) =>
{
    var count = await service.GetTotalCountAsync();
    return Results.Json(new Dictionary<string, object> { ["count"] = count }, TodoJsonContext.Default.DictionaryStringObject);
});

app.MapPut("/api/todos/batch/priority", async (BatchPriorityUpdateRequest request, ITodoService service) =>
{
    if (request.Ids?.Count == 0)
        return Results.Json(new ErrorResponse("ID列表不能为空"), TodoJsonContext.Default.ErrorResponse, statusCode: 400);

    // 转换为JSON数组格式供SQLite的json_each使用
    var idsJson = JsonSerializer.Serialize(request.Ids, TodoJsonContext.Default.ListInt64);
    var updated = await service.UpdatePriorityBatchAsync(idsJson, request.NewPriority, DateTime.UtcNow);
    return Results.Json(new Dictionary<string, object> { ["updated"] = updated }, TodoJsonContext.Default.DictionaryStringObject);
});

app.MapPost("/api/todos/archive-expired", async (ITodoService service) =>
{
    var now = DateTime.UtcNow;
    var archived = await service.ArchiveExpiredTasksAsync(now, isCompleted: false, completedAt: now, updatedAt: now);
    return Results.Json(new Dictionary<string, object> { ["archived"] = archived }, TodoJsonContext.Default.DictionaryStringObject);
});

// 数据库初始化
using var scope = app.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<DatabaseService>().InitializeDatabaseAsync();

Console.WriteLine("✅ 启动成功: http://localhost:5000");
app.Run();
