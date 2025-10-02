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
[JsonSerializable(typeof(List<long>))]
[JsonSerializable(typeof(CreateTodoRequest))]
[JsonSerializable(typeof(UpdateTodoRequest))]
[JsonSerializable(typeof(BatchPriorityUpdateRequest))]
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
