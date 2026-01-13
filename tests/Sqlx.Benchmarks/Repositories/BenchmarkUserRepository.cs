using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using System.Data;
using System.Data.Common;

namespace Sqlx.Benchmarks.Repositories;

/// <summary>
/// Manually implemented repository for benchmark testing.
/// This simulates what the source generator would produce.
/// </summary>
public class BenchmarkUserRepository
{
    private readonly SqliteConnection _connection;
    
    // Static PlaceholderContext - shared across all methods
    private static readonly Sqlx.PlaceholderContext _placeholderContext = new Sqlx.PlaceholderContext(
        dialect: Sqlx.SqlDefine.SQLite,
        tableName: "users",
        columns: new[]
        {
            new Sqlx.ColumnMeta("id", "Id", DbType.Int64, false),
            new Sqlx.ColumnMeta("name", "Name", DbType.String, false),
            new Sqlx.ColumnMeta("email", "Email", DbType.String, false),
            new Sqlx.ColumnMeta("age", "Age", DbType.Int32, false),
            new Sqlx.ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
            new Sqlx.ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            new Sqlx.ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, true),
            new Sqlx.ColumnMeta("balance", "Balance", DbType.Decimal, false),
            new Sqlx.ColumnMeta("description", "Description", DbType.String, true),
            new Sqlx.ColumnMeta("score", "Score", DbType.Int32, false),
        });
    
    // Static SqlTemplate fields - prepared once at initialization
    private static readonly Sqlx.SqlTemplate _getByIdTemplate = Sqlx.SqlTemplate.Prepare(
        "SELECT {{columns}} FROM {{table}} WHERE id = @id",
        _placeholderContext);
    
    private static readonly Sqlx.SqlTemplate _getAllTemplate = Sqlx.SqlTemplate.Prepare(
        "SELECT {{columns}} FROM {{table}} {{limit --param limit}}",
        _placeholderContext);
    
    private static readonly Sqlx.SqlTemplate _insertTemplate = Sqlx.SqlTemplate.Prepare(
        "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})",
        _placeholderContext);
    
    private static readonly Sqlx.SqlTemplate _updateTemplate = Sqlx.SqlTemplate.Prepare(
        "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
        _placeholderContext);
    
    private static readonly Sqlx.SqlTemplate _countTemplate = Sqlx.SqlTemplate.Prepare(
        "SELECT COUNT(*) FROM {{table}}",
        _placeholderContext);
    
    private static readonly Sqlx.SqlTemplate _getByMinAgeTemplate = Sqlx.SqlTemplate.Prepare(
        "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge",
        _placeholderContext);
    
    private static readonly Sqlx.SqlTemplate _getWhereTemplate = Sqlx.SqlTemplate.Prepare(
        "SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} {{limit --param limit}}",
        _placeholderContext);
    
    private static readonly Sqlx.SqlTemplate _getPagedTemplate = Sqlx.SqlTemplate.Prepare(
        "SELECT {{columns}} FROM {{table}} {{limit --param pageSize}} {{offset --param offset}}",
        _placeholderContext);
    
    public BenchmarkUserRepository(SqliteConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<BenchmarkUser?> GetByIdAsync(long id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _getByIdTemplate.Sql;
        AddParameter(cmd, "@id", id);
        
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadUser(reader);
        }
        return null;
    }
    
    public async Task<List<BenchmarkUser>> GetAllAsync(int limit)
    {
        var result = new List<BenchmarkUser>(limit);
        
        var dynamicParams = new Dictionary<string, object?> { ["limit"] = limit };
        var sql = _getAllTemplate.Render(dynamicParams);
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(ReadUser(reader));
        }
        return result;
    }
    
    public async Task<long> InsertAndGetIdAsync(BenchmarkUser entity)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _insertTemplate.Sql + "; SELECT last_insert_rowid()";
        
        AddParameter(cmd, "@name", entity.Name);
        AddParameter(cmd, "@email", entity.Email);
        AddParameter(cmd, "@age", entity.Age);
        AddParameter(cmd, "@is_active", entity.IsActive ? 1 : 0);
        AddParameter(cmd, "@created_at", entity.CreatedAt.ToString("O"));
        AddParameter(cmd, "@updated_at", entity.UpdatedAt?.ToString("O") ?? (object)DBNull.Value);
        AddParameter(cmd, "@balance", (double)entity.Balance);
        AddParameter(cmd, "@description", entity.Description ?? (object)DBNull.Value);
        AddParameter(cmd, "@score", entity.Score);
        
        var result = await cmd.ExecuteScalarAsync();
        return (long)result!;
    }
    
    public async Task<int> UpdateAsync(BenchmarkUser entity)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _updateTemplate.Sql;
        
        AddParameter(cmd, "@id", entity.Id);
        AddParameter(cmd, "@name", entity.Name);
        AddParameter(cmd, "@email", entity.Email);
        AddParameter(cmd, "@age", entity.Age);
        AddParameter(cmd, "@is_active", entity.IsActive ? 1 : 0);
        AddParameter(cmd, "@created_at", entity.CreatedAt.ToString("O"));
        AddParameter(cmd, "@updated_at", entity.UpdatedAt?.ToString("O") ?? (object)DBNull.Value);
        AddParameter(cmd, "@balance", (double)entity.Balance);
        AddParameter(cmd, "@description", entity.Description ?? (object)DBNull.Value);
        AddParameter(cmd, "@score", entity.Score);
        
        return await cmd.ExecuteNonQueryAsync();
    }
    
    public async Task<long> CountAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _countTemplate.Sql;
        var result = await cmd.ExecuteScalarAsync();
        return (long)result!;
    }
    
    public async Task<List<BenchmarkUser>> GetByMinAgeAsync(int minAge)
    {
        var result = new List<BenchmarkUser>();
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _getByMinAgeTemplate.Sql;
        AddParameter(cmd, "@minAge", minAge);
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(ReadUser(reader));
        }
        return result;
    }
    
    public async Task<List<BenchmarkUser>> GetWhereAsync(string whereClause, int limit)
    {
        var result = new List<BenchmarkUser>(limit);
        
        var dynamicParams = new Dictionary<string, object?> 
        { 
            ["predicate"] = whereClause,
            ["limit"] = limit 
        };
        var sql = _getWhereTemplate.Render(dynamicParams);
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(ReadUser(reader));
        }
        return result;
    }
    
    public async Task<List<BenchmarkUser>> GetPagedAsync(int pageSize, int offset)
    {
        var result = new List<BenchmarkUser>(pageSize);
        
        var dynamicParams = new Dictionary<string, object?> 
        { 
            ["pageSize"] = pageSize,
            ["offset"] = offset 
        };
        var sql = _getPagedTemplate.Render(dynamicParams);
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(ReadUser(reader));
        }
        return result;
    }
    
    private static BenchmarkUser ReadUser(DbDataReader reader)
    {
        return new BenchmarkUser
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2),
            Age = reader.GetInt32(3),
            IsActive = reader.GetInt64(4) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(5)),
            UpdatedAt = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
            Balance = (decimal)reader.GetDouble(7),
            Description = reader.IsDBNull(8) ? null : reader.GetString(8),
            Score = reader.GetInt32(9)
        };
    }
    
    private static void AddParameter(SqliteCommand cmd, string name, object value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        cmd.Parameters.Add(param);
    }
}
