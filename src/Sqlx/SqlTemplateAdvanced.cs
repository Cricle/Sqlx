// -----------------------------------------------------------------------
// <copyright file="SqlTemplateAdvanced.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// 高性能、安全且可扩展的SQL模板增强功能
    /// 专注于减少代码量、提高性能、确保安全性
    /// </summary>
    public static class SqlTemplateAdvanced
    {
        // 编译后的模板缓存，避免重复解析
        private static readonly ConcurrentDictionary<string, CompiledSqlTemplate> _templateCache = new();
        
        // 预编译的正则表达式，提高性能
        private static readonly Regex _variableRegex = new(@"\{\{(\w+)(?::([^}]+))?\}\}", RegexOptions.Compiled);
        private static readonly Regex _conditionalRegex = new(@"\{\{if\s+([^}]+)\}\}(.*?)\{\{endif\}\}", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex _loopRegex = new(@"\{\{each\s+(\w+)\s+in\s+(\w+)\}\}(.*?)\{\{endeach\}\}", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// 渲染模板的快速路径，支持条件、循环和内置函数
        /// </summary>
        /// <param name="template">模板字符串</param>
        /// <param name="parameters">参数对象</param>
        /// <param name="options">选项</param>
        /// <returns>渲染后的SQL模板</returns>
        public static SqlTemplate Render<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(string template, T parameters, SqlTemplateOptions? options = null)
        {
            options ??= SqlTemplateOptions.Default;
            
            // 使用缓存的编译模板
            var compiled = GetOrCompileTemplate(template, options);
            return compiled.Execute(parameters);
        }

        /// <summary>
        /// 编译模板为可重用的对象
        /// </summary>
        /// <param name="template">模板字符串</param>
        /// <param name="options">选项</param>
        /// <returns>编译后的模板</returns>
        public static CompiledSqlTemplate Compile(string template, SqlTemplateOptions? options = null)
        {
            options ??= SqlTemplateOptions.Default;
            var cacheKey = $"{template}|{options.GetHashCode()}";
            
            return _templateCache.GetOrAdd(cacheKey, _ => new CompiledSqlTemplate(template, options));
        }

        private static CompiledSqlTemplate GetOrCompileTemplate(string template, SqlTemplateOptions options)
        {
            if (!options.EnableCaching)
            {
                return new CompiledSqlTemplate(template, options);
            }
            
            var cacheKey = $"{template}|{options.GetHashCode()}";
            return _templateCache.GetOrAdd(cacheKey, _ => new CompiledSqlTemplate(template, options));
        }
    }

    /// <summary>
    /// 编译后的SQL模板，支持高性能执行
    /// </summary>
    public sealed class CompiledSqlTemplate
    {
        private readonly string _template;
        private readonly SqlTemplateOptions _options;
        private readonly List<TemplateOperation> _operations;

        internal CompiledSqlTemplate(string template, SqlTemplateOptions options)
        {
            _template = template;
            _options = options;
            _operations = CompileOperations(template);
        }

        /// <summary>
        /// 执行模板
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="parameters">参数对象</param>
        /// <returns>渲染后的SQL模板</returns>
        public SqlTemplate Execute<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(T parameters)
        {
            var context = new ExecutionContext(parameters, _options);
            var result = ExecuteOperations(context);
            return new SqlTemplate(result.Sql, result.Parameters);
        }

        private List<TemplateOperation> CompileOperations(string template)
        {
            var operations = new List<TemplateOperation>();
            var position = 0;

            while (position < template.Length)
            {
                // 查找下一个模板表达式
                var nextExpression = template.IndexOf("{{", position);
                
                if (nextExpression == -1)
                {
                    // 剩余的文本
                    if (position < template.Length)
                    {
                        operations.Add(new TextOperation(template.Substring(position)));
                    }
                    break;
                }

                // 添加前面的文本
                if (nextExpression > position)
                {
                    operations.Add(new TextOperation(template.Substring(position, nextExpression - position)));
                }

                // 解析表达式
                var expressionEnd = template.IndexOf("}}", nextExpression + 2);
                if (expressionEnd == -1)
                {
                    throw new InvalidOperationException($"Unclosed expression at position {nextExpression}");
                }

                var expression = template.Substring(nextExpression + 2, expressionEnd - nextExpression - 2).Trim();
                operations.Add(ParseExpression(expression, template, ref expressionEnd));
                
                position = expressionEnd + 2;
            }

            return operations;
        }

        private TemplateOperation ParseExpression(string expression, string fullTemplate, ref int endPosition)
        {
            // 条件表达式
            if (expression.StartsWith("if "))
            {
                return ParseConditional(expression.Substring(3).Trim(), fullTemplate, ref endPosition);
            }

            // 循环表达式
            if (expression.StartsWith("each "))
            {
                return ParseLoop(expression.Substring(5).Trim(), fullTemplate, ref endPosition);
            }

            // 函数调用
            if (expression.Contains('(') && expression.Contains(')'))
            {
                return new FunctionOperation(expression);
            }

            // 简单变量
            return new VariableOperation(expression);
        }

        private TemplateOperation ParseConditional(string condition, string fullTemplate, ref int endPosition)
        {
            // 查找对应的endif
            var depth = 1;
            var searchPos = endPosition + 2;
            var contentStart = endPosition + 2;
            
            while (depth > 0 && searchPos < fullTemplate.Length)
            {
                var nextIf = fullTemplate.IndexOf("{{if ", searchPos);
                var nextEndIf = fullTemplate.IndexOf("{{endif}}", searchPos);
                
                if (nextEndIf == -1)
                {
                    throw new InvalidOperationException($"Unclosed if statement: {condition}");
                }

                if (nextIf != -1 && nextIf < nextEndIf)
                {
                    depth++;
                    searchPos = nextIf + 5;
                }
                else
                {
                    depth--;
                    if (depth == 0)
                    {
                        var content = fullTemplate.Substring(contentStart, nextEndIf - contentStart);
                        endPosition = nextEndIf + 7; // 跳过 "{{endif}}"
                        return new ConditionalOperation(condition, content);
                    }
                    searchPos = nextEndIf + 9;
                }
            }

            throw new InvalidOperationException($"Malformed if statement: {condition}");
        }

        private TemplateOperation ParseLoop(string expression, string fullTemplate, ref int endPosition)
        {
            // 解析 "item in collection" 格式
            var parts = expression.Split(new string[] { " in " }, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                throw new InvalidOperationException($"Invalid each expression: {expression}");
            }

            var itemName = parts[0].Trim();
            var collectionName = parts[1].Trim();

            // 查找对应的endeach
            var endeachPos = fullTemplate.IndexOf("{{endeach}}", endPosition + 2);
            if (endeachPos == -1)
            {
                throw new InvalidOperationException($"Unclosed each statement: {expression}");
            }

            var content = fullTemplate.Substring(endPosition + 2, endeachPos - endPosition - 2);
            endPosition = endeachPos + 9; // 跳过 "{{endeach}}"
            
            return new LoopOperation(itemName, collectionName, content);
        }

        internal ExecutionResult ExecuteOperations(ExecutionContext context)
        {
            var sql = new StringBuilder();
            var parameters = new Dictionary<string, object?>();

            foreach (var operation in _operations)
            {
                var result = operation.Execute(context);
                sql.Append(result.Sql);
                
                foreach (var param in result.Parameters)
                {
                    parameters[param.Key] = param.Value;
                }
            }

            return new ExecutionResult(sql.ToString(), parameters);
        }
    }

    /// <summary>
    /// SQL模板选项
    /// </summary>
    public sealed class SqlTemplateOptions
    {
        /// <summary>
        /// 默认的SQL模板选项实例。
        /// </summary>
        public static readonly SqlTemplateOptions Default = new();

        /// <summary>
        /// 是否启用参数化查询（默认true）
        /// </summary>
        public bool UseParameterizedQueries { get; set; } = true;

        /// <summary>
        /// 是否启用安全模式（防止SQL注入，默认true）
        /// </summary>
        public bool SafeMode { get; set; } = true;

        /// <summary>
        /// 是否启用模板缓存（默认true）
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// SQL方言
        /// </summary>
        public SqlDialectType Dialect { get; set; } = SqlDialectType.SqlServer;

        /// <summary>
        /// 自定义函数
        /// </summary>
        public Dictionary<string, Func<object?[], object?>> CustomFunctions { get; set; } = new();

        /// <summary>
        /// 获取对象的哈希码。
        /// </summary>
        /// <returns>对象的哈希码。</returns>
        public override int GetHashCode()
        {
#if NET5_0_OR_GREATER
            return HashCode.Combine(UseParameterizedQueries, SafeMode, EnableCaching, Dialect);
#else
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + UseParameterizedQueries.GetHashCode();
                hash = hash * 23 + SafeMode.GetHashCode();
                hash = hash * 23 + EnableCaching.GetHashCode();
                hash = hash * 23 + Dialect.GetHashCode();
                return hash;
            }
#endif
        }
    }

    /// <summary>
    /// SQL方言类型
    /// </summary>
    public enum SqlDialectType
    {
        /// <summary>
        /// Microsoft SQL Server数据库。
        /// </summary>
        SqlServer,

        /// <summary>
        /// MySQL数据库。
        /// </summary>
        MySql,

        /// <summary>
        /// PostgreSQL数据库。
        /// </summary>
        PostgreSql,

        /// <summary>
        /// SQLite数据库。
        /// </summary>
        SQLite,

        /// <summary>
        /// Oracle数据库。
        /// </summary>
        Oracle
    }

    /// <summary>
    /// 执行上下文
    /// </summary>
    internal sealed class ExecutionContext
    {
        private readonly Dictionary<string, object?> _variables;
        internal readonly SqlTemplateOptions _options;
        private int _parameterCounter;

        public ExecutionContext(object? parameters, SqlTemplateOptions options)
        {
            _variables = ExtractVariables(parameters);
            _options = options;
            _parameterCounter = 0;
        }

        public object? GetVariable(string name) => _variables.TryGetValue(name, out var value) ? value : null;
        
        public string CreateParameter(object? value)
        {
            if (!_options.UseParameterizedQueries)
            {
                return FormatValue(value);
            }

            var paramName = $"p{_parameterCounter++}";
            Parameters[paramName] = value;
            return $"@{paramName}";
        }

        public Dictionary<string, object?> Parameters { get; } = new();

#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075", 
            Justification = "This is a flexible template engine that requires reflection. Users should ensure their objects are AOT-compatible when needed.")]
#endif
        private Dictionary<string, object?> ExtractVariables(object? obj)
        {
            var result = new Dictionary<string, object?>();
            
            if (obj == null) return result;

            try
            {
                var properties = obj.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    result[prop.Name] = prop.GetValue(obj);
                }
            }
            catch
            {
                // 忽略反射错误，返回空字典
            }

            return result;
        }

        private string FormatValue(object? value)
        {
            return value switch
            {
                null => "NULL",
                string s => _options.SafeMode ? $"'{s.Replace("'", "''")}'" : $"'{s}'",
                bool b => b ? "1" : "0",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                _ => value.ToString() ?? "NULL"
            };
        }

        public string QuoteIdentifier(string identifier)
        {
            return _options.Dialect switch
            {
                SqlDialectType.SqlServer => $"[{identifier}]",
                SqlDialectType.MySql => $"`{identifier}`",
                SqlDialectType.PostgreSql => $"\"{identifier}\"",
                SqlDialectType.SQLite => $"\"{identifier}\"",
                SqlDialectType.Oracle => $"\"{identifier}\"",
                _ => identifier
            };
        }
    }

    /// <summary>
    /// 执行结果
    /// </summary>
    internal readonly struct ExecutionResult
    {
        public string Sql { get; }
        public IReadOnlyDictionary<string, object?> Parameters { get; }

        public ExecutionResult(string sql, IReadOnlyDictionary<string, object?> parameters)
        {
            Sql = sql;
            Parameters = parameters;
        }
    }

    /// <summary>
    /// 模板操作基类
    /// </summary>
    internal abstract class TemplateOperation
    {
        public abstract ExecutionResult Execute(ExecutionContext context);
    }

    /// <summary>
    /// 文本操作
    /// </summary>
    internal sealed class TextOperation : TemplateOperation
    {
        private readonly string _text;

        public TextOperation(string text) => _text = text;

        public override ExecutionResult Execute(ExecutionContext context)
        {
            return new ExecutionResult(_text, new Dictionary<string, object?>());
        }
    }

    /// <summary>
    /// 变量操作
    /// </summary>
    internal sealed class VariableOperation : TemplateOperation
    {
        private readonly string _variableName;
        private readonly string? _format;

        public VariableOperation(string expression)
        {
            var parts = expression.Split(':');
            _variableName = parts[0].Trim();
            _format = parts.Length > 1 ? parts[1].Trim() : null;
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            var value = context.GetVariable(_variableName);
            var sql = context.CreateParameter(value);
            return new ExecutionResult(sql, context.Parameters);
        }
    }

    /// <summary>
    /// 条件操作
    /// </summary>
    internal sealed class ConditionalOperation : TemplateOperation
    {
        private readonly string _condition;
        private readonly string _content;

        public ConditionalOperation(string condition, string content)
        {
            _condition = condition;
            _content = content;
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            if (EvaluateCondition(context))
            {
                // 需要递归处理内容中的模板表达式
                var innerTemplate = new CompiledSqlTemplate(_content, context._options);
                return innerTemplate.ExecuteOperations(context);
            }
            return new ExecutionResult("", new Dictionary<string, object?>());
        }

        private bool EvaluateCondition(ExecutionContext context)
        {
            var value = context.GetVariable(_condition);
            return value switch
            {
                null => false,
                bool b => b,
                string s => !string.IsNullOrWhiteSpace(s),
                int i => i != 0,
                _ => true
            };
        }
    }

    /// <summary>
    /// 循环操作
    /// </summary>
    internal sealed class LoopOperation : TemplateOperation
    {
        private readonly string _itemName;
        private readonly string _collectionName;
        private readonly string _content;

        public LoopOperation(string itemName, string collectionName, string content)
        {
            _itemName = itemName;
            _collectionName = collectionName;
            _content = content;
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            var collection = context.GetVariable(_collectionName);
            if (collection is not System.Collections.IEnumerable enumerable)
            {
                return new ExecutionResult("", new Dictionary<string, object?>());
            }

            var sql = new StringBuilder();
            var index = 0;

            foreach (var item in enumerable)
            {
                // 简化的循环处理 - 直接替换参数
                var itemSql = _content.Replace($"{{{{{_itemName}}}}}", context.CreateParameter(item));
                itemSql = itemSql.Replace("{{index}}", index.ToString());
                sql.Append(itemSql);
                index++;
            }

            return new ExecutionResult(sql.ToString(), context.Parameters);
        }
    }

    /// <summary>
    /// 函数操作
    /// </summary>
    internal sealed class FunctionOperation : TemplateOperation
    {
        private readonly string _functionName;
        private readonly string[] _arguments;

        public FunctionOperation(string expression)
        {
            var parenIndex = expression.IndexOf('(');
            _functionName = expression.Substring(0, parenIndex).Trim();
            var argsString = expression.Substring(parenIndex + 1, expression.LastIndexOf(')') - parenIndex - 1);
            _arguments = string.IsNullOrWhiteSpace(argsString) 
                ? Array.Empty<string>() 
                : argsString.Split(new char[] { ',' }).Select(s => s.Trim()).ToArray();
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            var args = _arguments.Select(arg => 
            {
                // 简单的参数解析
                if (arg.StartsWith("\"") && arg.EndsWith("\"") && arg.Length >= 2)
                    return arg.Substring(1, arg.Length - 2);
                return context.GetVariable(arg);
            }).ToArray();

            var result = _functionName.ToLowerInvariant() switch
            {
                "upper" => args.FirstOrDefault()?.ToString()?.ToUpperInvariant(),
                "lower" => args.FirstOrDefault()?.ToString()?.ToLowerInvariant(),
                "trim" => args.FirstOrDefault()?.ToString()?.Trim(),
                "len" => args.FirstOrDefault()?.ToString()?.Length.ToString(),
                "table" => context.QuoteIdentifier(args.FirstOrDefault()?.ToString() ?? ""),
                "column" => context.QuoteIdentifier(args.FirstOrDefault()?.ToString() ?? ""),
                "join" => JoinFunction(args),
                _ => ""
            };

            return new ExecutionResult(result ?? "", new Dictionary<string, object?>());
        }

        private string JoinFunction(object?[] args)
        {
            if (args.Length < 2) return "";
            
            var separator = args[0]?.ToString() ?? "";
            var collection = args[1];
            
            if (collection is System.Collections.IEnumerable enumerable && !(collection is string))
            {
                return string.Join(separator, enumerable.Cast<object>());
            }
            
            return collection?.ToString() ?? "";
        }
    }
}
