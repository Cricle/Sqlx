using Microsoft.Data.Sqlite;
using Sqlx.Benchmarks.Models;
using System.Data;
using System.Data.Common;

namespace Sqlx.Benchmarks.Repositories;

/// <summary>
/// Repository using Sqlx SqlTemplate for SQL generation.
/// Simulates what source generator would produce.
/// </summary>
public class BenchmarkUserRepository
{
    private readonly SqliteConnection _connection;
    
    private static readonly Sqlx.PlaceholderContext _context = new(
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
    
    private static readonly Sqlx.SqlTemplate _getByIdTemplate = Sqlx.SqlTemplate.Prepare(
        "SELECT {{columns}} FROM {{table}} WHERE id = @id", _context);
    
    private static readonly Sqlx.SqlTemplate _getAllTemplate = Sqlx.SqlTemplate.Prepare(
        "SELECT {{columns}} FROM {{table}} {{limit --param limit}}", _context);
    
    private static readonly Sqlx.SqlTemplate _insertTemplate = Sqlx.SqlTemplate.Prepare(
        "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}}); SELECT last_insert_rowid()", _context);
    
    private static readonly Sqlx.SqlTemplate _updateTemplate = Sqlx.SqlTemplate.Prepare(
        "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", _context);
    
    private static readonly Sqlx.SqlTemplate _countTemplate = Sqlx.SqlTemplate.Prepare(
        "SELECT COUNT(*) FROM {{table}}", _context);
    
    private static readonly Sqlx.SqlTemplate _getByMinAgeTemplate = Sqlx.SqlTemplate.Prepare(
        "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", _context);
    
    private static readonly Sqlx.SqlTemplate _getPagedTemplate = Sqlx.SqlTemplate.Prepare(
        "SELECT {{columns}} FROM {{table}} {{limit --param pageSize}} {{offset --param offset}}", _context);
    
    public BenchmarkUserRepository(SqliteConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<BenchmarkUser?> GetByIdAsync(long id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _getByIdTemplate.Sql;
        AddParam(cmd, "@id", id);
        
        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadUser(reader) : null;
    }
    
    public async Task<List<BenchmarkUser>> GetAllAsync(int limit)
    {
        var sql = _getAllTemplate.Render(new Dictionary<string, object?> { ["limit"] = limit });
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        
        var result = new List<BenchmarkUser>(limit);
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
        cmd.CommandText = _insertTemplate.Sql;
        BindEntity(cmd, entity);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }
    
    public async Task<int> UpdateAsync(BenchmarkUser entity)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _updateTemplate.Sql;
        BindEntity(cmd, entity);
        AddParam(cmd, "@id", entity.Id);
        return await cmd.ExecuteNonQueryAsync();
    }
    
    public async Task<long> CountAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _countTemplate.Sql;
        return (long)(await cmd.ExecuteScalarAsync())!;
    }
    
    public async Task<List<BenchmarkUser>> GetByMinAgeAsync(int minAge)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _getByMinAgeTemplate.Sql;
        AddParam(cmd, "@minAge", minAge);
        
        var result = new List<BenchmarkUser>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(ReadUser(reader));
        }
        return result;
    }
    
    public async Task<List<BenchmarkUser>> GetPagedAsync(int pageSize, int offset)
    {
        var sql = _getPagedTemplate.Render(new Dictionary<string, object?> 
        { 
            ["pageSize"] = pageSize,
            ["offset"] = offset 
        });
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        
        var result = new List<BenchmarkUser>(pageSize);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(ReadUser(reader));
        }
        return result;
    }
    
    // Simulates generated IResultReader
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
    
    // Simulates generated IParameterBinder
    private static void BindEntity(DbCommand cmd, BenchmarkUser entity)
    {
        AddParam(cmd, "@name", entity.Name);
        AddParam(cmd, "@email", entity.Email);
        AddParam(cmd, "@age", entity.Age);
        AddParam(cmd, "@is_active", entity.IsActive ? 1 : 0);
        AddParam(cmd, "@created_at", entity.CreatedAt.ToString("O"));
        AddParam(cmd, "@updated_at", entity.UpdatedAt?.ToString("O") ?? (object)DBNull.Value);
        AddParam(cmd, "@balance", (double)entity.Balance);
        AddParam(cmd, "@description", entity.Description ?? (object)DBNull.Value);
        AddParam(cmd, "@score", entity.Score);
    }
    
    private static void AddParam(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
