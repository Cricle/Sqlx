// -----------------------------------------------------------------------
// <copyright file="AdvancedSqlTemplateDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;
using SqlxDemo.Models;

namespace SqlxDemo.Services
{
    /// <summary>
    /// 演示高级SQL模板引擎功能
    /// 支持条件、循环、函数等高级语法
    /// </summary>
    public class AdvancedSqlTemplateDemo
    {
        private readonly SqliteConnection _connection;

        public AdvancedSqlTemplateDemo(SqliteConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// 演示条件模板语法
        /// </summary>
        public async Task<List<User>> GetUsersWithConditionalFilterAsync(UserSearchRequest request)
        {
            // 使用条件语法构建动态WHERE子句
            var template = @"
                SELECT Id, Name, Email, Age, IsActive 
                FROM User 
                WHERE 1=1
                {{if hasNameFilter}}
                    AND Name LIKE {{namePattern}}
                {{endif}}
                {{if hasAgeFilter}}
                    AND Age >= {{minAge}}
                {{endif}}
                {{if activeOnly}}
                    AND IsActive = 1
                {{endif}}
                ORDER BY {{column(orderBy)}}";

            var sqlTemplate = SqlTemplate.Render(template, new
            {
                hasNameFilter = !string.IsNullOrEmpty(request.NameFilter),
                namePattern = $"%{request.NameFilter}%",
                hasAgeFilter = request.MinAge.HasValue,
                minAge = request.MinAge,
                activeOnly = request.ActiveOnly,
                orderBy = request.OrderBy ?? "Name"
            });
            
            // 执行查询...
            return new List<User>(); // 示例返回
        }

        /// <summary>
        /// 演示循环模板语法
        /// </summary>
        public async Task<int> BulkInsertUsersAsync(List<User> users)
        {
            var template = @"
                INSERT INTO User (Name, Email, Age, IsActive) VALUES
                {{each item in users}}
                    ({{item}}, {{item}}, {{item}}, {{item}})
                {{endeach}}";

            var sqlTemplate = SqlTemplate.Render(template, new { users });
            
            // 执行插入...
            return users.Count;
        }

        /// <summary>
        /// 演示函数模板语法
        /// </summary>
        public async Task<List<User>> GetUsersWithFormattingAsync(string[] columns, string tableName)
        {
            var template = @"
                SELECT {{join("","", columns)}}
                FROM {{table(tableName)}}
                WHERE {{column(""Name"")}} IS NOT NULL
                ORDER BY {{upper(orderColumn)}}";

            var sqlTemplate = SqlTemplate.Render(template, new
            {
                columns = columns,
                tableName = tableName,
                orderColumn = "name"
            });
            
            return new List<User>();
        }

        /// <summary>
        /// 演示编译后模板的重用
        /// </summary>
        public async Task DemonstrateCompiledTemplateReuseAsync()
        {
            // 编译模板一次，多次使用
            var compiled = SqlTemplate.Compile(@"
                SELECT * FROM {{table(tableName)}}
                {{if hasFilter}}
                WHERE {{column(filterColumn)}} = {{filterValue}}
                {{endif}}");

            // 多次执行不同参数
            var templates = new[]
            {
                compiled.Execute(new { tableName = "User", hasFilter = false }),
                compiled.Execute(new { tableName = "Order", hasFilter = true, filterColumn = "Status", filterValue = "Active" }),
                compiled.Execute(new { tableName = "Product", hasFilter = true, filterColumn = "IsActive", filterValue = true })
            };

            foreach (var template in templates)
            {
                System.Console.WriteLine($"SQL: {template.Sql}");
                System.Console.WriteLine($"Parameters: {template.Parameters.Count}");
            }
        }
    }

    /// <summary>
    /// 用户搜索请求
    /// </summary>
    public class UserSearchRequest
    {
        public string? NameFilter { get; set; }
        public int? MinAge { get; set; }
        public bool ActiveOnly { get; set; }
        public string? OrderBy { get; set; }
    }

}
