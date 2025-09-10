using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.Core
{
    /// <summary>
    /// 高级连接管理器 - 提供智能重试、连接池和健康监控
    /// </summary>
    public static class AdvancedConnectionManager
    {
        private static readonly ConcurrentDictionary<string, DbConnection> _connectionPool = new ConcurrentDictionary<string, DbConnection>();
        private static readonly ConcurrentDictionary<string, CircuitBreaker> _circuitBreakers = new ConcurrentDictionary<string, CircuitBreaker>();
        private static readonly ConnectionMetrics _metrics = new ConnectionMetrics();

        /// <summary>
        /// 确保连接打开
        /// </summary>
        public static async Task EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken = default)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }
        }

        /// <summary>
        /// 获取连接健康状况
        /// </summary>
        public static ConnectionHealth GetConnectionHealth(DbConnection connection)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var isHealthy = connection.State == ConnectionState.Open;
                stopwatch.Stop();

                return new ConnectionHealth
                {
                    IsHealthy = isHealthy,
                    State = connection.State,
                    Database = connection.Database ?? string.Empty,
                    ResponseTime = stopwatch.Elapsed
                };
            }
            catch
            {
                stopwatch.Stop();
                return new ConnectionHealth
                {
                    IsHealthy = false,
                    State = connection.State,
                    ResponseTime = stopwatch.Elapsed
                };
            }
        }

        /// <summary>
        /// 使用重试机制执行操作
        /// </summary>
        public static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxAttempts = 3)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt == maxAttempts)
                        break;

                    var delay = CalculateExponentialBackoff(attempt);
                    await Task.Delay(delay);
                }
            }

            throw lastException!;
        }

        /// <summary>
        /// 使用断路器执行操作
        /// </summary>
        public static async Task<T> ExecuteWithCircuitBreakerAsync<T>(string operationName, Func<Task<T>> operation)
        {
            var circuitBreaker = _circuitBreakers.GetOrAdd(operationName, _ => new CircuitBreaker());

            if (circuitBreaker.State == CircuitBreakerState.Open)
            {
                throw new CircuitBreakerOpenException($"Circuit breaker is open for operation: {operationName}");
            }

            try
            {
                var result = await operation();
                circuitBreaker.RecordSuccess();
                return result;
            }
            catch
            {
                circuitBreaker.RecordFailure();
                throw;
            }
        }

        /// <summary>
        /// 计算指数退避延迟
        /// </summary>
        public static TimeSpan CalculateExponentialBackoff(int attempt)
        {
            var baseDelay = TimeSpan.FromMilliseconds(100);
            var exponentialDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

            // 添加抖动以防止雷鸣般的群体效应
            return CalculateJitteredDelay(exponentialDelay, attempt);
        }

        /// <summary>
        /// 计算带抖动的延迟
        /// </summary>
        public static TimeSpan CalculateJitteredDelay(TimeSpan baseDelay, int attempt)
        {
            var random = new Random();
            var jitterFactor = 0.1; // 10% 抖动
            var jitter = random.NextDouble() * jitterFactor * baseDelay.TotalMilliseconds;

            return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds + jitter);
        }

        /// <summary>
        /// 获取池化连接
        /// </summary>
        public static DbConnection GetPooledConnection(string connectionString)
        {
            // 简化的连接池实现
            var key = $"pool_{connectionString.GetHashCode()}";
            return _connectionPool.GetOrAdd(key, _ => CreateConnection(connectionString));
        }

        /// <summary>
        /// 将连接返回到池中
        /// </summary>
        public static void ReturnToPool(DbConnection connection)
        {
            // 简化实现：实际应该验证连接状态并可能重置
            if (connection.State == ConnectionState.Open)
            {
                // 连接仍然有效，保持在池中
            }
        }

        /// <summary>
        /// 恢复连接
        /// </summary>
        public static async Task RecoverConnectionAsync(DbConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
        }

        /// <summary>
        /// 获取连接指标
        /// </summary>
        public static ConnectionMetrics GetConnectionMetrics()
        {
            return _metrics;
        }

        private static DbConnection CreateConnection(string connectionString)
        {
            // 简化实现：这里应该根据连接字符串类型创建适当的连接
            // 为了避免依赖问题，返回一个基础的DbConnection实现
            throw new NotImplementedException("Connection creation should be implemented based on connection string type");
        }
    }

    /// <summary>
    /// 连接健康状况
    /// </summary>
    public class ConnectionHealth
    {
        public bool IsHealthy { get; set; }
        public ConnectionState State { get; set; }
        public string Database { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
        public TimeSpan ResponseTime { get; set; }

        public override string ToString()
        {
            return $"Healthy: {IsHealthy}, State: {State}, Database: {Database}, ResponseTime: {ResponseTime.TotalMilliseconds:F1}ms";
        }
    }

    /// <summary>
    /// 连接指标
    /// </summary>
    public class ConnectionMetrics
    {
        public int TotalConnections { get; set; }
        public int ActiveConnections { get; set; }
        public int FailedConnections { get; set; }
        public TimeSpan AverageConnectionTime { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"Total: {TotalConnections}, Active: {ActiveConnections}, Failed: {FailedConnections}, AvgTime: {AverageConnectionTime.TotalMilliseconds:F1}ms";
        }
    }

    /// <summary>
    /// 断路器
    /// </summary>
    public class CircuitBreaker
    {
        private int _failureCount = 0;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private readonly int _failureThreshold = 3;
        private readonly TimeSpan _timeout = TimeSpan.FromMinutes(1);

        public CircuitBreakerState State
        {
            get
            {
                if (_failureCount >= _failureThreshold)
                {
                    if (DateTime.UtcNow - _lastFailureTime > _timeout)
                    {
                        return CircuitBreakerState.HalfOpen;
                    }
                    return CircuitBreakerState.Open;
                }
                return CircuitBreakerState.Closed;
            }
        }

        public void RecordSuccess()
        {
            _failureCount = 0;
        }

        public void RecordFailure()
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 断路器状态
    /// </summary>
    public enum CircuitBreakerState
    {
        Closed,
        Open,
        HalfOpen
    }

    /// <summary>
    /// 断路器打开异常
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }
}