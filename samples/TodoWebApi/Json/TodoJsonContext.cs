using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TodoWebApi.Models;

namespace TodoWebApi.Json;

/// <summary>
/// AOT 兼容的 JSON 序列化上下文
/// 为所有模型类型提供源代码生成的序列化支持
/// </summary>
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(List<Todo>))]
[JsonSerializable(typeof(CreateTodoRequest))]
[JsonSerializable(typeof(UpdateTodoRequest))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ApiInfoResponse))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class TodoJsonContext : JsonSerializerContext
{
}

/// <summary>
/// 错误响应模型
/// </summary>
public record ErrorResponse(string Error);

/// <summary>
/// API 信息响应模型
/// </summary>
public record ApiInfoResponse(
    string ApplicationName,
    string Version,
    string Environment,
    string DatabaseType,
    object ApiEndpoints,
    DateTime GeneratedAt
);

/// <summary>
/// 创建TODO请求模型
/// </summary>
public record CreateTodoRequest(
    string Title,
    string? Description = null,
    bool IsCompleted = false,
    int Priority = 2,
    DateTime? DueDate = null,
    string? Tags = null,
    int? EstimatedMinutes = null
);

/// <summary>
/// 更新TODO请求模型
/// </summary>
public record UpdateTodoRequest(
    string Title,
    string? Description,
    bool IsCompleted,
    int Priority,
    DateTime? DueDate,
    string? Tags,
    int? EstimatedMinutes
);
