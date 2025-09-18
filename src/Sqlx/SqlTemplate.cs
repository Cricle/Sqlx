// -----------------------------------------------------------------------
// <copyright file="SqlTemplate.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System.Collections.Generic;
using System.Reflection;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// 表示SQL模板定义（纯模板，不包含参数值）
    /// 可重用的模板定义，通过Execute方法绑定参数创建ParameterizedSql执行实例
    /// 
    /// 注意：为了向后兼容，还保留了原有的Parameters属性，但推荐使用新的设计模式
    /// </summary>
    public readonly record struct SqlTemplate(string Sql, IReadOnlyDictionary<string, object?> Parameters)
    {
        /// <summary>
        /// 空的SqlTemplate，用于表示没有参数的SQL
        /// </summary>
        public static readonly SqlTemplate Empty = new(string.Empty, new Dictionary<string, object?>());

        #region 新设计：纯模板定义（推荐使用）

        /// <summary>
        /// 创建纯模板定义（不包含参数值）- 推荐的新设计
        /// </summary>
        /// <param name="sql">SQL模板字符串</param>
        /// <returns>纯模板定义</returns>
        public static SqlTemplate Parse(string sql)
        {
            return new SqlTemplate(sql, new Dictionary<string, object?>());
        }

        /// <summary>
        /// 执行模板并绑定参数 - 创建ParameterizedSql执行实例
        /// </summary>
        /// <param name="parameters">参数对象</param>
        /// <returns>参数化SQL执行实例</returns>
        public ParameterizedSql Execute(object? parameters = null)
        {
            return ParameterizedSql.Create(Sql, parameters);
        }

        /// <summary>
        /// 执行模板并绑定字典参数
        /// </summary>
        /// <param name="parameters">参数字典</param>
        /// <returns>参数化SQL执行实例</returns>
        public ParameterizedSql Execute(Dictionary<string, object?> parameters)
        {
            return ParameterizedSql.CreateWithDictionary(Sql, parameters);
        }

        /// <summary>
        /// 创建流式参数绑定器
        /// </summary>
        /// <returns>参数绑定器</returns>
        public SqlTemplateBuilder Bind()
        {
            return new SqlTemplateBuilder(this);
        }

        /// <summary>
        /// 检查是否为纯模板（不包含参数值）
        /// </summary>
        public bool IsPureTemplate => Parameters.Count == 0;

        #endregion

        #region 向后兼容：原有API（标记为过时）
        
        /// <summary>
        /// 创建一个新的SqlTemplate（已过时，推荐使用Parse+Execute模式）
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数字典</param>
        /// <returns>SqlTemplate实例</returns>
        [System.Obsolete("Use SqlTemplate.Parse(sql).Execute(parameters) for better template reuse, or ParameterizedSql.Create for direct parameter binding.")]
        public static SqlTemplate Create(string sql, Dictionary<string, object?> parameters)
        {
            return new SqlTemplate(sql, parameters);
        }
        
        /// <summary>
        /// 创建一个新的SqlTemplate（使用泛型参数）- AOT兼容（已过时）
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数对象</param>
        /// <returns>SqlTemplate实例</returns>
        [System.Obsolete("Use SqlTemplate.Parse(sql).Execute(parameters) for better template reuse, or ParameterizedSql.Create for direct parameter binding.")]
        public static SqlTemplate Create<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(string sql, T? parameters = default)
        {
            var paramDict = new Dictionary<string, object?>();
            
            if (parameters != null)
            {
                var properties = typeof(T).GetProperties();
                foreach (var prop in properties)
                {
                    paramDict[prop.Name] = prop.GetValue(parameters);
                }
            }
            
            return new SqlTemplate(sql, paramDict);
        }

        /// <summary>
        /// 创建一个新的SqlTemplate（使用匿名对象）- 反射版本（已过时）
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数对象</param>
        /// <returns>SqlTemplate实例</returns>
        [System.Obsolete("Use SqlTemplate.Parse(sql).Execute(parameters) for better template reuse, or ParameterizedSql.Create for direct parameter binding.")]
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("This method uses reflection to examine parameter properties. Consider using the generic Create<T> method for AOT compatibility.")]
#endif
        public static SqlTemplate Create(string sql, object? parameters = null)
        {
            return CreateFromObject(sql, parameters);
        }

        /// <summary>
        /// 内部方法：从object创建SqlTemplate
        /// </summary>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("This method uses reflection to examine parameter properties.")]
#endif
        private static SqlTemplate CreateFromObject(string sql, object? parameters)
        {
            var paramDict = new Dictionary<string, object?>();
            
            if (parameters != null)
            {
                var properties = parameters.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    paramDict[prop.Name] = prop.GetValue(parameters);
                }
            }
            
            return new SqlTemplate(sql, paramDict);
        }

        /// <summary>
        /// 创建SqlTemplate（使用泛型参数字典）
        /// </summary>
        /// <typeparam name="T">参数值类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数字典</param>
        /// <returns>SqlTemplate实例</returns>
        public static SqlTemplate Create<T>(string sql, Dictionary<string, T> parameters)
        {
            var paramDict = new Dictionary<string, object?>();
            foreach (var kvp in parameters)
            {
                paramDict[kvp.Key] = kvp.Value;
            }
            return new SqlTemplate(sql, paramDict);
        }

        /// <summary>
        /// 创建SqlTemplate（使用单个参数）
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="paramName">参数名</param>
        /// <param name="paramValue">参数值</param>
        /// <returns>SqlTemplate实例</returns>
        public static SqlTemplate Create<T>(string sql, string paramName, T paramValue)
        {
            var paramDict = new Dictionary<string, object?> { [paramName] = paramValue };
            return new SqlTemplate(sql, paramDict);
        }

        /// <summary>
        /// 创建SqlTemplate（使用两个参数）
        /// </summary>
        /// <typeparam name="T1">第一个参数类型</typeparam>
        /// <typeparam name="T2">第二个参数类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="param1Name">第一个参数名</param>
        /// <param name="param1Value">第一个参数值</param>
        /// <param name="param2Name">第二个参数名</param>
        /// <param name="param2Value">第二个参数值</param>
        /// <returns>SqlTemplate实例</returns>
        public static SqlTemplate Create<T1, T2>(string sql, string param1Name, T1 param1Value, string param2Name, T2 param2Value)
        {
            var paramDict = new Dictionary<string, object?> 
            { 
                [param1Name] = param1Value,
                [param2Name] = param2Value
            };
            return new SqlTemplate(sql, paramDict);
        }

        /// <summary>
        /// 使用高级模板语法编译模板
        /// 支持条件、循环、函数等高级语法
        /// </summary>
        /// <param name="templateSql">模板SQL字符串</param>
        /// <param name="options">编译选项</param>
        /// <returns>编译后的模板</returns>
        public static CompiledSqlTemplate Compile(string templateSql, SqlTemplateOptions? options = null)
        {
            return SqlTemplateAdvanced.Compile(templateSql, options);
        }

        /// <summary>
        /// 使用高级模板语法渲染模板（泛型版本）
        /// 支持条件、循环、函数等高级语法
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="templateSql">模板SQL字符串</param>
        /// <param name="parameters">参数对象</param>
        /// <param name="options">编译选项</param>
        /// <returns>渲染后的SQL模板</returns>
        public static SqlTemplate Render<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(string templateSql, T parameters, SqlTemplateOptions? options = null)
        {
            return SqlTemplateAdvanced.Render(templateSql, parameters, options);
        }

        /// <summary>
        /// Returns a string representation of the SqlTemplate.
        /// </summary>
        public override string ToString()
        {
            var paramCount = Parameters?.Count ?? 0;
            return $"SqlTemplate {{ Sql = {Sql}, Parameters = {paramCount} params }}";
        }

        #endregion
    }

    /// <summary>
    /// 流式SQL模板参数绑定器 - 用于构建ParameterizedSql
    /// </summary>
    public sealed class SqlTemplateBuilder
    {
        private readonly SqlTemplate _template;
        private readonly Dictionary<string, object?> _parameters = new();

        internal SqlTemplateBuilder(SqlTemplate template)
        {
            _template = template;
        }

        /// <summary>
        /// 绑定参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="name">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns>构建器实例</returns>
        public SqlTemplateBuilder Param<T>(string name, T value)
        {
            _parameters[name] = value;
            return this;
        }

        /// <summary>
        /// 批量绑定参数
        /// </summary>
        /// <param name="parameters">参数对象</param>
        /// <returns>构建器实例</returns>
        public SqlTemplateBuilder Params(object? parameters)
        {
            if (parameters == null) return this;

            // 优先支持字典类型
            if (parameters is Dictionary<string, object?> dict)
            {
                foreach (var kvp in dict)
                {
                    _parameters[kvp.Key] = kvp.Value;
                }
                return this;
            }

            // 支持匿名对象和简单对象 - 但在AOT模式下可能失败
#pragma warning disable IL2075 // 暂时忽略AOT警告，这是设计上的权衡
            try
            {
                var type = parameters.GetType();
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    if (prop.CanRead)
                    {
                        _parameters[prop.Name] = prop.GetValue(parameters);
                    }
                }
            }
            catch
            {
                // AOT环境下反射失败是预期的，直接忽略
            }
#pragma warning restore IL2075

            return this;
        }

        /// <summary>
        /// 构建最终的ParameterizedSql
        /// </summary>
        /// <returns>参数化SQL实例</returns>
        public ParameterizedSql Build()
        {
            return new ParameterizedSql(_template.Sql, _parameters);
        }
    }
}
