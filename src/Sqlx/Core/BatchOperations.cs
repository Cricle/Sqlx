using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlx.Core
{
    /// <summary>
    /// 高性能批量操作工具类
    /// </summary>
    public static class BatchOperations
    {
        /// <summary>
        /// 批量插入数据
        /// </summary>
        public static async Task<BatchOperationResult> InsertBatchAsync<T>(
            DbConnection connection, 
            string tableName, 
            IEnumerable<T> items, 
            int batchSize = 1000,
            bool autoOptimizeBatchSize = false,
            bool continueOnError = false,
            DbTransaction? transaction = null)
        {
            var result = new BatchOperationResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var itemList = items.ToList();
                if (!itemList.Any())
                {
                    result.Success = true;
                    return result;
                }

                // 自动优化批次大小
                if (autoOptimizeBatchSize)
                {
                    batchSize = OptimizeBatchSize(itemList.Count);
                    result.OptimalBatchSize = batchSize;
                }

                var batches = CreateBatches(itemList, batchSize);
                result.BatchCount = batches.Count;

                foreach (var batch in batches)
                {
                    try
                    {
                        var affectedRows = await InsertBatch(connection, tableName, batch, transaction);
                        result.AffectedRows += affectedRows;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Batch error: {ex.Message}");
                        if (!continueOnError)
                        {
                            result.Success = false;
                            return result;
                        }
                    }
                }

                result.Success = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                result.Success = false;
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
            }

            return result;
        }

        /// <summary>
        /// 批量更新数据
        /// </summary>
        public static async Task<BatchOperationResult> UpdateBatchAsync<T>(
            DbConnection connection,
            string tableName,
            IEnumerable<T> items,
            string keyColumn,
            DbTransaction? transaction = null)
        {
            var result = new BatchOperationResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var itemList = items.ToList();
                if (!itemList.Any())
                {
                    result.Success = true;
                    return result;
                }

                foreach (var item in itemList)
                {
                    try
                    {
                        var affectedRows = await UpdateSingle(connection, tableName, item, keyColumn, transaction);
                        result.AffectedRows += affectedRows;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Update error: {ex.Message}");
                    }
                }

                result.Success = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                result.Success = false;
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
            }

            return result;
        }

        /// <summary>
        /// 批量更新（条件更新）
        /// </summary>
        public static async Task<BatchOperationResult> UpdateBatchAsync(
            DbConnection connection,
            string tableName,
            object updateData,
            DbTransaction? transaction = null)
        {
            var result = new BatchOperationResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 这里应该解析 updateData 对象并构建 SQL
                // 简化实现：假设 updateData 包含 SetClause, WhereClause, Parameters
                var properties = updateData.GetType().GetProperties();
                var setClause = properties.FirstOrDefault(p => p.Name == "SetClause")?.GetValue(updateData)?.ToString() ?? "";
                var whereClause = properties.FirstOrDefault(p => p.Name == "WhereClause")?.GetValue(updateData)?.ToString() ?? "";
                var parameters = properties.FirstOrDefault(p => p.Name == "Parameters")?.GetValue(updateData);

                if (!string.IsNullOrEmpty(setClause))
                {
                    var sql = $"UPDATE {tableName} SET {setClause}";
                    if (!string.IsNullOrEmpty(whereClause))
                    {
                        sql += $" WHERE {whereClause}";
                    }

                    using var cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = sql;

                    // 添加参数
                    if (parameters != null)
                    {
                        AddParameters(cmd, parameters);
                    }

                    result.AffectedRows = await cmd.ExecuteNonQueryAsync();
                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                result.Success = false;
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
            }

            return result;
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public static async Task<BatchOperationResult> DeleteBatchAsync<T>(
            DbConnection connection,
            string tableName,
            string keyColumn,
            IEnumerable<T> keyValues,
            DbTransaction? transaction = null)
        {
            var result = new BatchOperationResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var keyList = keyValues.ToList();
                if (!keyList.Any())
                {
                    result.Success = true;
                    return result;
                }

                // 构建 IN 子句
                var parameterNames = keyList.Select((_, i) => $"@key{i}").ToList();
                var inClause = string.Join(", ", parameterNames);
                var sql = $"DELETE FROM {tableName} WHERE {keyColumn} IN ({inClause})";

                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = sql;

                // 添加参数
                for (int i = 0; i < keyList.Count; i++)
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = $"@key{i}";
                    param.Value = (object?)keyList[i] ?? DBNull.Value;
                    cmd.Parameters.Add(param);
                }

                result.AffectedRows = await cmd.ExecuteNonQueryAsync();
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                result.Success = false;
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
            }

            return result;
        }

        /// <summary>
        /// 批量删除（条件删除）
        /// </summary>
        public static async Task<BatchOperationResult> DeleteBatchAsync(
            DbConnection connection,
            string tableName,
            string whereClause,
            object parameters,
            DbTransaction? transaction = null)
        {
            var result = new BatchOperationResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var sql = $"DELETE FROM {tableName} WHERE {whereClause}";

                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = sql;

                // 添加参数
                AddParameters(cmd, parameters);

                result.AffectedRows = await cmd.ExecuteNonQueryAsync();
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                result.Success = false;
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
            }

            return result;
        }

        private static int OptimizeBatchSize(int totalItems)
        {
            // 简单的批次大小优化算法
            if (totalItems < 100) return totalItems;
            if (totalItems < 1000) return 100;
            if (totalItems < 10000) return 1000;
            return 5000;
        }

        private static List<List<T>> CreateBatches<T>(List<T> items, int batchSize)
        {
            var batches = new List<List<T>>();
            for (int i = 0; i < items.Count; i += batchSize)
            {
                batches.Add(items.Skip(i).Take(batchSize).ToList());
            }
            return batches;
        }

        private static async Task<int> InsertBatch<T>(DbConnection connection, string tableName, List<T> batch, DbTransaction? transaction)
        {
            if (!batch.Any()) return 0;

            // 获取属性信息
            var properties = typeof(T).GetProperties().Where(p => p.Name != "Id").ToList();
            var columnNames = properties.Select(p => p.Name).ToList();

            // 构建批量插入 SQL
            var sql = new StringBuilder();
            sql.AppendLine($"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES");

            var valuesClauses = new List<string>();

            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;

            for (int i = 0; i < batch.Count; i++)
            {
                var item = batch[i];
                var paramNames = columnNames.Select(col => $"@{col}_{i}").ToList();
                valuesClauses.Add($"({string.Join(", ", paramNames)})");

                // 添加参数
                for (int j = 0; j < properties.Count; j++)
                {
                    var prop = properties[j];
                    var paramName = $"@{prop.Name}_{i}";
                    var value = prop.GetValue(item) ?? DBNull.Value;
                    
                    var param = cmd.CreateParameter();
                    param.ParameterName = paramName;
                    param.Value = value;
                    cmd.Parameters.Add(param);
                }
            }

            sql.AppendLine(string.Join(",\n", valuesClauses));
            cmd.CommandText = sql.ToString();

            return await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<int> UpdateSingle<T>(DbConnection connection, string tableName, T item, string keyColumn, DbTransaction? transaction)
        {
            var properties = typeof(T).GetProperties().Where(p => p.Name != keyColumn).ToList();
            var keyProperty = typeof(T).GetProperty(keyColumn);

            if (keyProperty == null) return 0;

            var setClauses = properties.Select(p => $"{p.Name} = @{p.Name}").ToList();
            var sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {keyColumn} = @{keyColumn}";

            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = sql;

            // 添加 SET 参数
            foreach (var prop in properties)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = $"@{prop.Name}";
                param.Value = prop.GetValue(item) ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }

            // 添加 WHERE 参数
            var keyParam = cmd.CreateParameter();
            keyParam.ParameterName = $"@{keyColumn}";
            keyParam.Value = keyProperty.GetValue(item) ?? DBNull.Value;
            cmd.Parameters.Add(keyParam);

            return await cmd.ExecuteNonQueryAsync();
        }

        private static void AddParameters(DbCommand cmd, object parameters)
        {
            if (parameters == null) return;

            var properties = parameters.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = $"@{prop.Name}";
                param.Value = prop.GetValue(parameters) ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }
        }
    }

    /// <summary>
    /// 批量操作结果
    /// </summary>
    public class BatchOperationResult
    {
        public bool Success { get; set; }
        public int AffectedRows { get; set; }
        public int BatchCount { get; set; }
        public int OptimalBatchSize { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ExecutionTime { get; set; }
    }
}
