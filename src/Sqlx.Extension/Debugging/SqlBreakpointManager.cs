using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Sqlx.Extension.Debugging
{
    /// <summary>
    /// SQL断点管理器
    /// </summary>
    public class SqlBreakpointManager
    {
        private static SqlBreakpointManager _instance;
        private static readonly object _lock = new object();
        private int _nextBreakpointId = 1;
        private readonly Dictionary<int, SqlBreakpointInfo> _breakpoints = new Dictionary<int, SqlBreakpointInfo>();

        /// <summary>
        /// 单例实例
        /// </summary>
        public static SqlBreakpointManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SqlBreakpointManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 断点添加事件
        /// </summary>
        public event EventHandler<SqlBreakpointInfo> BreakpointAdded;

        /// <summary>
        /// 断点移除事件
        /// </summary>
        public event EventHandler<int> BreakpointRemoved;

        /// <summary>
        /// 断点命中事件
        /// </summary>
        public event EventHandler<SqlBreakpointHitEventArgs> BreakpointHit;

        /// <summary>
        /// 断点更新事件
        /// </summary>
        public event EventHandler<SqlBreakpointInfo> BreakpointUpdated;

        private SqlBreakpointManager() { }

        /// <summary>
        /// 添加断点
        /// </summary>
        public SqlBreakpointInfo AddBreakpoint(string filePath, int lineNumber, string methodName, string sqlTemplate)
        {
            lock (_lock)
            {
                var breakpoint = new SqlBreakpointInfo
                {
                    Id = _nextBreakpointId++,
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    MethodName = methodName,
                    SqlTemplate = sqlTemplate,
                    IsEnabled = true
                };

                _breakpoints[breakpoint.Id] = breakpoint;
                BreakpointAdded?.Invoke(this, breakpoint);

                return breakpoint;
            }
        }

        /// <summary>
        /// 移除断点
        /// </summary>
        public bool RemoveBreakpoint(int breakpointId)
        {
            lock (_lock)
            {
                if (_breakpoints.Remove(breakpointId))
                {
                    BreakpointRemoved?.Invoke(this, breakpointId);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 获取断点
        /// </summary>
        public SqlBreakpointInfo GetBreakpoint(int breakpointId)
        {
            lock (_lock)
            {
                return _breakpoints.TryGetValue(breakpointId, out var breakpoint) ? breakpoint : null;
            }
        }

        /// <summary>
        /// 获取所有断点
        /// </summary>
        public IReadOnlyList<SqlBreakpointInfo> GetAllBreakpoints()
        {
            lock (_lock)
            {
                return _breakpoints.Values.ToList();
            }
        }

        /// <summary>
        /// 获取指定文件的断点
        /// </summary>
        public IReadOnlyList<SqlBreakpointInfo> GetBreakpoints(string filePath)
        {
            lock (_lock)
            {
                return _breakpoints.Values
                    .Where(bp => bp.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>
        /// 检查是否有断点
        /// </summary>
        public bool HasBreakpoint(string filePath, int lineNumber)
        {
            lock (_lock)
            {
                return _breakpoints.Values.Any(bp =>
                    bp.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase) &&
                    bp.LineNumber == lineNumber &&
                    bp.IsEnabled);
            }
        }

        /// <summary>
        /// 触发断点
        /// </summary>
        public bool TriggerBreakpoint(string methodName, string generatedSql, Dictionary<string, object> parameters)
        {
            lock (_lock)
            {
                var breakpoint = _breakpoints.Values
                    .FirstOrDefault(bp => bp.MethodName == methodName && bp.IsEnabled);

                if (breakpoint == null)
                    return false;

                // 更新断点信息
                breakpoint.GeneratedSql = generatedSql;
                breakpoint.Parameters = parameters;
                breakpoint.HitCount++;
                breakpoint.LastHitAt = DateTime.Now;

                // 检查条件
                if (!string.IsNullOrEmpty(breakpoint.Condition))
                {
                    if (!EvaluateCondition(breakpoint.Condition, parameters))
                        return false;
                }

                // 检查命中计数
                if (breakpoint.TargetHitCount > 0)
                {
                    if (breakpoint.HitCount < breakpoint.TargetHitCount)
                        return false;
                }

                // 触发断点命中事件
                var args = new SqlBreakpointHitEventArgs
                {
                    Breakpoint = breakpoint,
                    ShouldBreak = !breakpoint.IsLogPoint
                };

                BreakpointHit?.Invoke(this, args);

                return args.ShouldBreak;
            }
        }

        /// <summary>
        /// 更新断点
        /// </summary>
        public bool UpdateBreakpoint(int breakpointId, Action<SqlBreakpointInfo> updateAction)
        {
            lock (_lock)
            {
                if (_breakpoints.TryGetValue(breakpointId, out var breakpoint))
                {
                    updateAction?.Invoke(breakpoint);
                    BreakpointUpdated?.Invoke(this, breakpoint);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 启用/禁用断点
        /// </summary>
        public bool SetBreakpointEnabled(int breakpointId, bool enabled)
        {
            return UpdateBreakpoint(breakpointId, bp => bp.IsEnabled = enabled);
        }

        /// <summary>
        /// 设置断点条件
        /// </summary>
        public bool SetBreakpointCondition(int breakpointId, string condition)
        {
            return UpdateBreakpoint(breakpointId, bp =>
            {
                bp.Condition = condition;
                bp.Type = string.IsNullOrEmpty(condition) ? BreakpointType.Line : BreakpointType.Conditional;
            });
        }

        /// <summary>
        /// 设置断点命中计数
        /// </summary>
        public bool SetBreakpointHitCount(int breakpointId, int targetHitCount)
        {
            return UpdateBreakpoint(breakpointId, bp =>
            {
                bp.TargetHitCount = targetHitCount;
                if (targetHitCount > 0)
                    bp.Type = BreakpointType.HitCount;
            });
        }

        /// <summary>
        /// 清除所有断点
        /// </summary>
        public void ClearAllBreakpoints()
        {
            lock (_lock)
            {
                var ids = _breakpoints.Keys.ToList();
                _breakpoints.Clear();
                foreach (var id in ids)
                {
                    BreakpointRemoved?.Invoke(this, id);
                }
            }
        }

        /// <summary>
        /// 评估条件表达式
        /// </summary>
        private bool EvaluateCondition(string condition, Dictionary<string, object> parameters)
        {
            try
            {
                // 简单的条件评估实现
                // 实际应该使用Roslyn Scripting或表达式树
                // 这里仅实现基本的参数比较
                
                // 示例: "id > 100", "@name == 'test'"
                foreach (var param in parameters)
                {
                    var paramName = param.Key.StartsWith("@") ? param.Key : "@" + param.Key;
                    condition = condition.Replace(paramName, param.Value?.ToString() ?? "null");
                }

                // TODO: 实现完整的表达式求值
                // 目前返回true以允许断点触发
                return true;
            }
            catch
            {
                return true; // 条件评估失败时允许断点触发
            }
        }
    }

    /// <summary>
    /// 断点命中事件参数
    /// </summary>
    public class SqlBreakpointHitEventArgs : EventArgs
    {
        /// <summary>
        /// 断点信息
        /// </summary>
        public SqlBreakpointInfo Breakpoint { get; set; }

        /// <summary>
        /// 是否应该暂停执行
        /// </summary>
        public bool ShouldBreak { get; set; }
    }
}

