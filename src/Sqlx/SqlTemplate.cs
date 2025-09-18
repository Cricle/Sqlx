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
    /// 表示SQL模板，包含参数化的SQL语句和参数字典
    /// 类似于EF Core的FromSql，但更轻量级
    /// </summary>
    public readonly record struct SqlTemplate(string Sql, IReadOnlyDictionary<string, object?> Parameters)
    {
        /// <summary>
        /// 空的SqlTemplate，用于表示没有参数的SQL
        /// </summary>
        public static readonly SqlTemplate Empty = new(string.Empty, new Dictionary<string, object?>());
        
        /// <summary>
        /// 创建一个新的SqlTemplate
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数字典</param>
        /// <returns>SqlTemplate实例</returns>
        public static SqlTemplate Create(string sql, Dictionary<string, object?> parameters)
        {
            return new SqlTemplate(sql, parameters);
        }
        
        /// <summary>
        /// 创建一个新的SqlTemplate（使用泛型参数）- AOT兼容
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数对象</param>
        /// <returns>SqlTemplate实例</returns>
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
        /// 创建一个新的SqlTemplate（使用匿名对象）- 反射版本
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数对象</param>
        /// <returns>SqlTemplate实例</returns>
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
        /// Returns a string representation of the SqlTemplate.
        /// </summary>
        public override string ToString()
        {
            var paramCount = Parameters?.Count ?? 0;
            return $"SqlTemplate {{ Sql = {Sql}, Parameters = {paramCount} params }}";
        }
    }
}
