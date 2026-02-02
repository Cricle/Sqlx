using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Sqlx.Annotations;

namespace TodoWebApi.Models;

/// <summary>
/// TODO项目模型 - 演示Sqlx ORM的核心功能
/// 支持多种数据类型、索引、约束和AOT编译
/// </summary>
[Sqlx]
[TableName("todos")]
public class Todo
{
    /// <summary>主键ID</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>TODO标题</summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>TODO描述</summary>
    public string? Description { get; set; }

    /// <summary>是否完成</summary>
    public bool IsCompleted { get; set; }

    /// <summary>优先级（1-低，2-中，3-高，4-紧急，5-最高）</summary>
    [Range(1, 5)]
    public int Priority { get; set; } = 2;

    /// <summary>截止日期</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>完成时间</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>标签（JSON格式存储）</summary>
    public string? Tags { get; set; }

    /// <summary>估计工作时间（分钟）</summary>
    public int? EstimatedMinutes { get; set; }

    /// <summary>实际工作时间（分钟）</summary>
    public int? ActualMinutes { get; set; }
}

/// <summary>
/// TODO分类模型
/// </summary>
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

    [StringLength(7)] // #RRGGBB格式
    public string Color { get; set; } = "#007bff";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// TODO与分类的关联模型
/// </summary>
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

/// <summary>
/// TODO统计信息（用于演示聚合查询）
/// </summary>
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

/// <summary>
/// 纯 Record 类型示例 - 不可变数据传输对象
/// 使用构造函数初始化，适合 API 响应和值对象
/// </summary>
[Sqlx, TableName("todo_snapshots")]
public record TodoSnapshot(
    long Id,
    string Title,
    bool IsCompleted,
    int Priority,
    DateTime CreatedAt
);

/// <summary>
/// 混合 Record 类型示例 - 核心字段不可变，扩展字段可变
/// 主构造函数参数为核心字段，额外属性为可选字段
/// </summary>
[Sqlx, TableName("todo_summaries")]
public record TodoSummary(long Id, string Title)
{
    public bool IsCompleted { get; set; }
    public int Priority { get; set; }
    public DateTime? DueDate { get; set; }
    
    // 只读属性 - 自动忽略
    public string Status => IsCompleted ? "完成" : "进行中";
}

/// <summary>
/// Struct 类型示例 - 轻量级值类型
/// 适合小型数据结构，减少堆分配
/// </summary>
[Sqlx, TableName("coordinates")]
public struct Coordinate
{
    public int X { get; set; }
    public int Y { get; set; }
    
    // 只读属性 - 自动忽略
    public double DistanceFromOrigin => Math.Sqrt(X * X + Y * Y);
}

/// <summary>
/// Struct Record 类型示例 - 不可变值类型
/// 零分配，适合高性能场景
/// </summary>
[Sqlx, TableName("points")]
public readonly record struct Point(int X, int Y)
{
    // 只读属性 - 自动忽略
    public double Distance => Math.Sqrt(X * X + Y * Y);
}

/// <summary>
/// 带只读属性的 Class 示例 - 演示自动过滤
/// 只读属性不会生成到 SQL 中
/// </summary>
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
    
    // 只读计算属性 - 自动忽略
    public string DisplayTitle => $"[{(IsCompleted ? "✓" : " ")}] {Title}";
    public int DaysOld => (DateTime.UtcNow - CreatedAt).Days;
    public bool IsRecent => DaysOld < 7;
}

/// <summary>
/// TODO查询过滤器
/// </summary>
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

/// <summary>
/// 批量更新优先级请求
/// </summary>
public record BatchPriorityUpdateRequest
{
    [Required]
    public List<long> Ids { get; init; } = new();

    [Range(1, 5)]
    public int NewPriority { get; init; }
}

/// <summary>
/// API信息响应
/// </summary>
public record ApiInfoResponse(
    string Name,
    string Version,
    string Environment,
    string Database,
    Dictionary<string, object> Endpoints,
    DateTime Timestamp);

/// <summary>
/// 错误响应
/// </summary>
public record ErrorResponse(string Message);

/// <summary>
/// 批量更新结果
/// </summary>
public record BatchUpdateResult(int UpdatedCount);

/// <summary>
/// 批量删除请求
/// </summary>
public record BatchDeleteRequest
{
    [Required]
    public List<long> Ids { get; init; } = new();
}

/// <summary>
/// 批量完成请求
/// </summary>
public record BatchCompleteRequest
{
    [Required]
    public List<long> Ids { get; init; } = new();
}

/// <summary>
/// 批量获取请求
/// </summary>
public record BatchGetRequest
{
    [Required]
    public List<long> Ids { get; init; } = new();
}

/// <summary>
/// 更新实际工作时间请求
/// </summary>
public record UpdateActualMinutesRequest
{
    [Range(0, int.MaxValue)]
    public int ActualMinutes { get; init; }
}

/// <summary>
/// 存在性检查结果
/// </summary>
public record ExistsResult(bool Exists);

/// <summary>
/// TODO 标题和优先级
/// </summary>
public record TodoTitlePriority(long Id, string Title, int Priority);

/// <summary>
/// TODO 统计信息
/// </summary>
public record TodoStatsResult(long Total, long Completed, long Pending, long HighPriority, double CompletionRate);

/// <summary>
/// 创建TODO请求
/// </summary>
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

/// <summary>
/// 更新TODO请求
/// </summary>
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

