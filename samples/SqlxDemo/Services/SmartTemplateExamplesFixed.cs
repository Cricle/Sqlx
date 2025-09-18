// -----------------------------------------------------------------------
// <copyright file="SmartTemplateExamplesFixed.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SqlxDemo.Services
{
    /// <summary>
    /// 智能SQL模板概念示例 - 展示如何让开发者更专注业务而非SQL细节
    /// 这是一个概念性示例，展示未来可能的API设计
    /// </summary>
    public class SmartTemplateExamplesFixed
    {
        #region 业务实体定义

        public class User
        {
            [Key]
            public int Id { get; set; }

            [Required]
            [StringLength(100)]
            public string FirstName { get; set; } = "";

            [Required]
            [StringLength(100)]
            public string LastName { get; set; } = "";

            [Required]
            [StringLength(255)]
            public string Email { get; set; } = "";

            public int Age { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }

            // 大型文本字段
            [StringLength(5000)]
            public string? Biography { get; set; }

            [StringLength(10000)]
            public string? InternalNotes { get; set; }

            // 关联属性
            public int? DepartmentId { get; set; }
            public string? ProfileImageUrl { get; set; }
        }

        public class UserSearchCriteria
        {
            public string? NameFilter { get; set; }
            public int? MinAge { get; set; }
            public int? MaxAge { get; set; }
            public bool? IsActive { get; set; }
            public DateTime? CreatedAfter { get; set; }
            public DateTime? CreatedBefore { get; set; }
            public int[]? DepartmentIds { get; set; }
            public string SortBy { get; set; } = "created_at";
            public bool SortDescending { get; set; } = true;
            public int PageSize { get; set; } = 20;
            public int PageNumber { get; set; } = 1;
        }

        #endregion

        #region 传统方式 vs 智能模板对比

        /// <summary>
        /// 传统方式 - 需要手写大量SQL，容易出错
        /// 示例SQL：
        /// SELECT u.id, u.first_name, u.last_name, u.email, u.age, 
        ///        u.is_active, u.created_at, u.updated_at, u.department_id
        /// FROM users u
        /// WHERE u.is_active = @isActive
        /// AND (@nameFilter IS NULL OR u.first_name LIKE @namePattern OR u.last_name LIKE @namePattern)
        /// AND (@minAge IS NULL OR u.age >= @minAge)
        /// AND (@maxAge IS NULL OR u.age <= @maxAge)
        /// ORDER BY u.created_at DESC
        /// LIMIT @pageSize OFFSET @offset
        /// </summary>
        public Task<List<User>> GetUsersTraditionalWayAsync(
            bool isActive,
            string? nameFilter,
            string? namePattern,
            int? minAge,
            int? maxAge,
            DateTime? createdAfter,
            DateTime? createdBefore,
            int pageSize,
            int offset)
        {
            throw new NotImplementedException("示例方法 - 仅用于展示概念");
        }

        /// <summary>
        /// 智能模板方式 - 专注业务逻辑，自动处理SQL细节
        /// 示例模板：
        /// SELECT {{columns:exclude=biography,internal_notes}}
        /// FROM {{table:auto}}
        /// WHERE {{filter:active}} 
        /// {{if hasNameFilter}}
        ///     AND ({{column:first_name}} LIKE {{namePattern}} OR {{column:last_name}} LIKE {{namePattern}})
        /// {{endif}}
        /// {{if hasAgeRange}}
        ///     AND {{column:age}} BETWEEN {{minAge}} AND {{maxAge}}
        /// {{endif}}
        /// {{sort:dynamic}} {{paginate}}
        /// </summary>
        public Task<List<User>> GetUsersSmartWayAsync(UserSearchCriteria criteria)
        {
            throw new NotImplementedException("示例方法 - 需要相应的源生成器支持");
        }

        #endregion

        #region 高级列名匹配模式示例

        /// <summary>
        /// 正则表达式匹配 - 选择所有ID相关的列
        /// 模板：SELECT {{columns:pattern=.*_?id$}} FROM {{table:auto}} WHERE {{column:is_active}} = 1
        /// </summary>
        public Task<List<object>> GetUserIdsAsync()
        {
            throw new NotImplementedException("示例方法");
        }

        /// <summary>
        /// 通配符匹配 - 选择所有时间相关的列
        /// 模板：SELECT {{columns:match=*_at}}, {{columns:match=created_*}} FROM {{table:auto}}
        /// </summary>
        public Task<List<object>> GetUserTimestampsAsync()
        {
            throw new NotImplementedException("示例方法");
        }

        /// <summary>
        /// 类型基础匹配 - 只选择字符串类型的列
        /// 模板：SELECT {{columns:type=string,exclude=internal_notes}} FROM {{table:auto}}
        /// </summary>
        public Task<List<object>> GetUserTextFieldsAsync()
        {
            throw new NotImplementedException("示例方法");
        }

        /// <summary>
        /// 智能分类匹配 - 自动选择显示用的列
        /// 模板：SELECT {{columns:auto=display}} FROM {{table:auto}}
        /// </summary>
        public Task<List<object>> GetUsersForDisplayAsync()
        {
            throw new NotImplementedException("示例方法");
        }

        #endregion

        #region 业务场景特化模板

        /// <summary>
        /// 用户搜索 - 专门针对搜索场景优化
        /// 模板：
        /// SELECT {{columns:auto=basic}} 
        /// FROM {{table:auto}}
        /// WHERE {{searchConditions:fields=first_name,last_name,email}}
        /// {{sort:relevance,created_at}} {{paginate:withTotal=true}}
        /// </summary>
        public Task<SearchResult<User>> SearchUsersAsync(string searchTerm, SearchOptions options)
        {
            throw new NotImplementedException("示例方法");
        }

        /// <summary>
        /// 用户统计报表 - 专门针对报表场景
        /// 模板：
        /// SELECT 
        ///     {{dateGroup:column=created_at,period=reportPeriod}} as period,
        ///     COUNT(*) as total_users,
        ///     COUNT(CASE WHEN {{column:is_active}} = 1 THEN 1 END) as active_users,
        ///     AVG({{column:age}}) as avg_age
        /// FROM {{table:auto}}
        /// WHERE {{dateRange:column=created_at}}
        /// GROUP BY {{dateGroup:column=created_at,period=reportPeriod}}
        /// </summary>
        public Task<List<UserStatistics>> GetUserStatisticsAsync(ReportRequest request)
        {
            throw new NotImplementedException("示例方法");
        }

        #endregion

        #region 支持类型定义

        public class SearchResult<T>
        {
            public List<T> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
        }

        public class SearchOptions
        {
            public UserSearchCriteria? Filters { get; set; }
            public int PageSize { get; set; } = 20;
            public int PageNumber { get; set; } = 1;
            public bool IncludeTotal { get; set; } = true;
        }

        public class UserStatistics
        {
            public DateTime Period { get; set; }
            public int TotalUsers { get; set; }
            public int ActiveUsers { get; set; }
            public double AvgAge { get; set; }
        }

        public class ReportRequest
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string ReportPeriod { get; set; } = "month"; // day, week, month, year
        }

        #endregion

        #region 实用工具方法

        /// <summary>
        /// 展示生成的SQL概念 - 用于理解智能模板的工作原理
        /// </summary>
        public void DemonstrateSmartTemplatesConcept()
        {
            Console.WriteLine("=== 智能模板概念演示 ===");
            Console.WriteLine();

            Console.WriteLine("传统方式需要手写的SQL:");
            Console.WriteLine(@"
                SELECT u.id, u.first_name, u.last_name, u.email, u.age, 
                       u.is_active, u.created_at, u.updated_at, u.department_id
                FROM users u
                WHERE u.is_active = @isActive
                AND (@nameFilter IS NULL OR u.first_name LIKE @namePattern OR u.last_name LIKE @namePattern)
                AND (@minAge IS NULL OR u.age >= @minAge)
                AND (@maxAge IS NULL OR u.age <= @maxAge)
                ORDER BY u.created_at DESC
                LIMIT @pageSize OFFSET @offset
            ");

            Console.WriteLine();
            Console.WriteLine("智能模板只需要写:");
            Console.WriteLine(@"
                SELECT {{columns:exclude=biography,internal_notes}}
                FROM {{table:auto}}
                WHERE {{filter:active,name,age}} 
                {{sort:created_at,desc}} {{paginate}}
            ");

            Console.WriteLine();
            Console.WriteLine("核心优势:");
            Console.WriteLine("✅ 减少80%的SQL代码量");
            Console.WriteLine("✅ 自动处理列名映射");
            Console.WriteLine("✅ 智能诊断和错误预防");
            Console.WriteLine("✅ 专注业务逻辑而非SQL细节");
            Console.WriteLine("✅ 自动生成安全的参数化查询");
        }

        #endregion
    }
}

