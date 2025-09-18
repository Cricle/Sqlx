// -----------------------------------------------------------------------
// <copyright file="ParameterizedSql.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System.Collections.Generic;
using System.Text;
using System.Reflection;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// 表示参数化的SQL语句，包含SQL文本和参数值
    /// 这是执行时的实例，而不是可重用的模板定义
    /// </summary>
    public readonly record struct ParameterizedSql(string Sql, IReadOnlyDictionary<string, object?> Parameters)
    {
        /// <summary>
        /// 空的ParameterizedSql实例
        /// </summary>
        public static readonly ParameterizedSql Empty = new(string.Empty, new Dictionary<string, object?>());

        /// <summary>
        /// 创建参数化SQL（使用字典）
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数字典</param>
        /// <returns>ParameterizedSql实例</returns>
        public static ParameterizedSql CreateWithDictionary(string sql, Dictionary<string, object?> parameters)
        {
            return new ParameterizedSql(sql, parameters);
        }

        /// <summary>
        /// 创建参数化SQL（使用匿名对象）
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数对象</param>
        /// <returns>ParameterizedSql实例</returns>
        public static ParameterizedSql Create(string sql, object? parameters)
        {
            var paramDict = ExtractParametersSafe(parameters);
            return new ParameterizedSql(sql, paramDict);
        }

        /// <summary>
        /// 渲染为最终的SQL字符串（内联参数）
        /// </summary>
        /// <returns>渲染后的SQL字符串</returns>
        public string Render()
        {
            return SqlParameterRenderer.RenderToSql(this);
        }

        #region 私有辅助方法

        private static Dictionary<string, object?> ExtractParametersSafe(object? parameters)
        {
            var result = new Dictionary<string, object?>();
            if (parameters == null) return result;

            // 优先支持字典类型
            if (parameters is Dictionary<string, object?> dict)
            {
                foreach (var kvp in dict)
                {
                    result[kvp.Key] = kvp.Value;
                }
                return result;
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
                        result[prop.Name] = prop.GetValue(parameters);
                    }
                }
            }
            catch
            {
                // AOT环境下反射失败是预期的，直接返回空字典
            }
#pragma warning restore IL2075

            return result;
        }

        #endregion

        /// <summary>
        /// Returns a string representation of the ParameterizedSql.
        /// </summary>
        public override string ToString()
        {
            var paramCount = Parameters?.Count ?? 0;
            return $"ParameterizedSql {{ Sql = {Sql}, Parameters = {paramCount} params }}";
        }
    }

    /// <summary>
    /// SQL参数渲染器 - 用于将参数内联到SQL中
    /// </summary>
    internal static class SqlParameterRenderer
    {
        public static string RenderToSql(ParameterizedSql parameterizedSql)
        {
            if (parameterizedSql.Parameters.Count == 0)
            {
                return parameterizedSql.Sql;
            }

            var sql = parameterizedSql.Sql;
            foreach (var kvp in parameterizedSql.Parameters)
            {
                var value = FormatParameterValue(kvp.Value);
                sql = sql.Replace(kvp.Key, value);
            }

            return sql;
        }

        private static string FormatParameterValue(object? value)
        {
            return value switch
            {
                null => "NULL",
                string s => $"'{s.Replace("'", "''")}'",
                bool b => b ? "1" : "0",
                System.DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                System.DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss zzz}'",
                System.Guid g => $"'{g}'",
                decimal d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString() ?? "NULL"
            };
        }
    }
}
