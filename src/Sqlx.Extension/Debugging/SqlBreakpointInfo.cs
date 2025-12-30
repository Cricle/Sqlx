using System;
using System.Collections.Generic;

namespace Sqlx.Extension.Debugging
{
    /// <summary>
    /// SQL断点信息
    /// </summary>
    public class SqlBreakpointInfo
    {
        /// <summary>
        /// 断点ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 方法名
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// SQL模板
        /// </summary>
        public string SqlTemplate { get; set; }

        /// <summary>
        /// 生成的SQL
        /// </summary>
        public string GeneratedSql { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 断点条件表达式
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// 命中计数
        /// </summary>
        public int HitCount { get; set; }

        /// <summary>
        /// 目标命中计数
        /// </summary>
        public int TargetHitCount { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 是否为日志断点（不暂停执行）
        /// </summary>
        public bool IsLogPoint { get; set; }

        /// <summary>
        /// 日志消息
        /// </summary>
        public string LogMessage { get; set; }

        /// <summary>
        /// 断点创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后命中时间
        /// </summary>
        public DateTime? LastHitAt { get; set; }

        /// <summary>
        /// 断点类型
        /// </summary>
        public BreakpointType Type { get; set; } = BreakpointType.Line;
    }

    /// <summary>
    /// 断点类型
    /// </summary>
    public enum BreakpointType
    {
        /// <summary>
        /// 行断点
        /// </summary>
        Line,

        /// <summary>
        /// 条件断点
        /// </summary>
        Conditional,

        /// <summary>
        /// 命中计数断点
        /// </summary>
        HitCount,

        /// <summary>
        /// 日志断点
        /// </summary>
        LogPoint
    }
}

