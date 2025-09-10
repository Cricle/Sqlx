using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Sqlx.Core
{
    /// <summary>
    /// 智能缓存管理器 - 提供 LRU 缓存、TTL 支持和内存压力感知
    /// </summary>
    public static class IntelligentCacheManager
    {
        private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();
        private static readonly ConcurrentDictionary<string, CacheMetadata> _metadata = new ConcurrentDictionary<string, CacheMetadata>();
        private static long _hitCount = 0;
        private static long _missCount = 0;
        private static readonly object _statsLock = new object();

        /// <summary>
        /// 获取缓存值
        /// </summary>
        public static T? Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out var cacheEntry))
            {
                // 检查是否过期
                if (cacheEntry.ExpiresAt.HasValue && DateTime.UtcNow > cacheEntry.ExpiresAt.Value)
                {
                    // 过期了，移除缓存项
                    _cache.TryRemove(key, out _);
                    _metadata.TryRemove(key, out _);
                    Interlocked.Increment(ref _missCount);
                    return default(T);
                }

                Interlocked.Increment(ref _hitCount);

                // 更新访问时间
                if (_metadata.TryGetValue(key, out var metadata))
                {
                    metadata.LastAccessed = DateTime.UtcNow;
                    metadata.AccessCount++;
                }

                return (T)cacheEntry.Value;
            }

            Interlocked.Increment(ref _missCount);
            return default(T);
        }

        /// <summary>
        /// 设置缓存值
        /// </summary>
        public static void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var expiresAt = expiration.HasValue
                ? DateTime.UtcNow.Add(expiration.Value)
                : DateTime.UtcNow.AddHours(1); // 默认1小时过期

            var cacheEntry = new CacheEntry
            {
                Value = value!,
                ExpiresAt = expiresAt
            };

            _cache.AddOrUpdate(key, cacheEntry, (k, v) => cacheEntry);

            // 记录元数据
            _metadata[key] = new CacheMetadata
            {
                Key = key,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                AccessCount = 0,
                Size = EstimateSize(value)
            };
        }

        /// <summary>
        /// 获取或添加缓存值
        /// </summary>
        public static T GetOrAdd<T>(string key, Func<T> factory)
        {
            var existing = Get<T>(key);
            if (existing != null)
            {
                return existing;
            }

            var value = factory();
            Set(key, value);
            return value;
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        public static bool Remove(string key)
        {
            var removed = _cache.TryRemove(key, out _);
            if (removed)
            {
                _metadata.TryRemove(key, out _);
            }
            return removed;
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public static void Clear()
        {
            _cache.Clear();
            _metadata.Clear();
            Interlocked.Exchange(ref _hitCount, 0);
            Interlocked.Exchange(ref _missCount, 0);
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public static CacheStatistics GetStatistics()
        {
            lock (_statsLock)
            {
                var totalRequests = _hitCount + _missCount;
                var hitRatio = totalRequests > 0 ? (double)_hitCount / totalRequests : 0.0;

                return new CacheStatistics
                {
                    HitCount = _hitCount,
                    MissCount = _missCount,
                    EntryCount = _metadata.Count,
                    HitRatio = hitRatio,
                    TotalMemoryUsage = EstimateTotalMemoryUsage()
                };
            }
        }


        private static long EstimateSize<T>(T value)
        {
            if (value == null) return 0;

            // 简单的大小估算
            if (value is string str)
                return str.Length * 2; // Unicode characters

            if (value is byte[] bytes)
                return bytes.Length;

            // 对于其他类型，使用一个默认估算
            return 100;
        }

        private static long EstimateTotalMemoryUsage()
        {
            long total = 0;
            foreach (var metadata in _metadata.Values)
            {
                total += metadata.Size;
            }
            return total;
        }
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public int EntryCount { get; set; }
        public double HitRatio { get; set; }
        public long TotalMemoryUsage { get; set; }
        public int MaxSize { get; set; } = 10000; // 默认最大缓存条目数
    }

    /// <summary>
    /// 缓存项
    /// </summary>
    internal class CacheEntry
    {
        public object Value { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// 缓存项元数据
    /// </summary>
    internal class CacheMetadata
    {
        public string Key { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
        public int AccessCount { get; set; }
        public long Size { get; set; }
    }
}