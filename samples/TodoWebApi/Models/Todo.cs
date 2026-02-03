using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Sqlx.Annotations;

namespace TodoWebApi.Models;

/// <summary>TODO项目模型 - 演示Sqlx ORM的核心功能，支持多种数据类型、索引、约束和AOT编译</summary>
[Sqlx]
[TableName("todos")]
public class Todo
{
    [Key]
    public long Id { get; set; }
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    [Range(1, 5)]
    public int Priority { get; set; } = 2;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? Tags { get; set; }
    public int? EstimatedMinutes { get; set; }
    public int? ActualMinutes { get; set; }
}

[Sqlx]
[TableName("categories")]
public class Category
{
    [Key]
    public long Id { get; set; }
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [StringLength(7)]
    public string Color { get; set; } = "#007bff";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

[Sqlx]
[TableName("todo_categories")]
public class TodoCategory
{
    [Key]
    public long Id { get; set; }
    public long TodoId { get; set; }
    public long CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public record TodoStats
{
    public int TotalTodos { get; init; }
    public int CompletedTodos { get; init; }
    public int PendingTodos { get; init; }
    public int HighPriorityTodos { get; init; }
    public int OverdueTodos { get; init; }
    public double CompletionRate { get; init; }
    public int TotalEstimatedMinutes { get; init; }
    public int TotalActualMinutes { get; init; }
}

// ===== 高级类型支持示例 =====
/// <summary>纯 Record 类型示例 - 不可变数据传输对象，使用构造函数初始化，适合 API 响应和值对象</summary>
[Sqlx, TableName("todo_snapshots")]
public record TodoSnapshot(long Id, string Title, bool IsCompleted, int Priority, DateTime CreatedAt);

/// <summary>混合 Record 类型示例 - 核心字段不可变，扩展字段可变，主构造函数参数为核心字段，额外属性为可选字段</summary>
[Sqlx, TableName("todo_summaries")]
public record TodoSummary(long Id, string Title)
{
    public bool IsCompleted { get; set; }
    public int Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status => IsCompleted ? "完成" : "进行中";
}

/// <summary>Struct 类型示例 - 轻量级值类型，适合小型数据结构，减少堆分配</summary>
[Sqlx, TableName("coordinates")]
public struct Coordinate
{
    public int X { get; set; }
    public int Y { get; set; }
    public double DistanceFromOrigin => Math.Sqrt(X * X + Y * Y);
}

/// <summary>Struct Record 类型示例 - 不可变值类型，零分配，适合高性能场景</summary>
[Sqlx, TableName("points")]
public readonly record struct Point(int X, int Y)
{
    public double Distance => Math.Sqrt(X * X + Y * Y);
}

/// <summary>带只读属性的 Class 示例 - 演示自动过滤，只读属性不会生成到 SQL 中</summary>
[Sqlx, TableName("todo_details")]
public class TodoDetail
{
    [Key]
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string DisplayTitle => $"[{(IsCompleted ? "✓" : " ")}] {Title}";
    public int DaysOld => (DateTime.UtcNow - CreatedAt).Days;
    public bool IsRecent => DaysOld < 7;
}

public record TodoFilter
{
    public bool? IsCompleted { get; init; }
    public int? Priority { get; init; }
    public long? CategoryId { get; init; }
    public DateTime? DueBefore { get; init; }
    public DateTime? DueAfter { get; init; }
    public string? SearchText { get; init; }
    public string? Tags { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
    public string? OrderBy { get; init; } = "created_at";
    public bool Descending { get; init; } = true;
}

public record BatchPriorityUpdateRequest
{
    [Required]
    public List<long> Ids { get; init; } = new();
    [Range(1, 5)]
    public int NewPriority { get; init; }
}

public record ApiInfoResponse(string Name, string Version, string Environment, string Database, Dictionary<string, object> Endpoints, DateTime Timestamp);
public record ErrorResponse(string Message);
public record BatchUpdateResult(int UpdatedCount);
public record BatchDeleteRequest { [Required] public List<long> Ids { get; init; } = new(); }
public record BatchCompleteRequest { [Required] public List<long> Ids { get; init; } = new(); }
public record BatchGetRequest { [Required] public List<long> Ids { get; init; } = new(); }
public record UpdateActualMinutesRequest { [Range(0, int.MaxValue)] public int ActualMinutes { get; init; } }
public record ExistsResult(bool Exists);
[Sqlx]
public record TodoTitlePriority(long Id, string Title, int Priority);
public record TodoStatsResult(long Total, long Completed, long Pending, long HighPriority, double CompletionRate);
public record CreateTodoRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsCompleted { get; init; } = false;
    [Range(1, 5)]
    public int Priority { get; init; } = 2;
    public DateTime? DueDate { get; init; }
    public string? Tags { get; init; }
    public int? EstimatedMinutes { get; init; }
}

public record UpdateTodoRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsCompleted { get; init; }
    [Range(1, 5)]
    public int Priority { get; init; } = 2;
    public DateTime? DueDate { get; init; }
    public string? Tags { get; init; }
    public int? EstimatedMinutes { get; init; }
}

