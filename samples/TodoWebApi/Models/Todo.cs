using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Sqlx.Annotations;

namespace TodoWebApi.Models;

/// <summary>
/// TODO项目模型 - 演示Sqlx ORM的核心功能
/// 支持多种数据类型、索引、约束和AOT编译
/// </summary>
[SqlxEntity]
[SqlxParameter]
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
[SqlxEntity]
[SqlxParameter]
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
[SqlxEntity]
[SqlxParameter]
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
