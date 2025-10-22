// 这个文件用于手动复制粘贴生成的代码，用于分析性能问题
// 1. 编译 Benchmark 项目
// 2. 从 obj/Release/net8.0/generated 目录复制生成的代码到这里
// 3. 分析生成的代码并与 Dapper 对比

/*
预期生成的代码应该类似：

public User? GetByIdSync(int id)
{
    User? __result__ = default!;
    IDbCommand? __cmd__ = null;

    if (_connection.State != ConnectionState.Open)
        _connection.Open();
    
    __cmd__ = _connection.CreateCommand();
    __cmd__.CommandText = "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM users WHERE id = @id";
    
    var param_id = __cmd__.CreateParameter();
    param_id.ParameterName = "@id";
    param_id.Value = id;
    param_id.DbType = DbType.Int32;
    __cmd__.Parameters.Add(param_id);

    using var reader = __cmd__.ExecuteReader();
    if (reader.Read())
    {
        __result__ = new User
        {
            Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
            Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            // ... 等等
        };
    }

    return __result__;
}
*/

