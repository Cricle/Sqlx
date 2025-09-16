using System;
using Sqlx.Annotations;

/// <summary>
/// SqlDefine 枚举特性使用演示
/// </summary>
public class SqlDefineEnumDemo
{
    static void Main()
    {
        Console.WriteLine("🎯 SqlDefine 枚举特性使用演示\n");

        // 演示所有枚举值的使用
        DemonstrateEnumUsage();
        
        // 演示向后兼容性
        DemonstrateBackwardCompatibility();
        
        // 演示实际使用场景
        DemonstrateRealWorldUsage();
        
        Console.WriteLine("\n✅ 演示完成！");
    }

    static void DemonstrateEnumUsage()
    {
        Console.WriteLine("📋 枚举特性使用演示:");
        
        var enumValues = (SqlDefineTypes[])Enum.GetValues(typeof(SqlDefineTypes));
        
        foreach (var enumValue in enumValues)
        {
            var attribute = new SqlDefineAttribute(enumValue);
            Console.WriteLine($"   {enumValue} -> DialectType: {attribute.DialectType}, DialectName: {attribute.DialectName}");
        }
        
        Console.WriteLine();
    }

    static void DemonstrateBackwardCompatibility()
    {
        Console.WriteLine("🔄 向后兼容性演示:");
        
        // 字符串构造函数
        var stringAttr = new SqlDefineAttribute("MySql");
        Console.WriteLine($"   字符串构造: \"MySql\" -> {stringAttr.DialectType}");
        
        // 枚举构造函数
        var enumAttr = new SqlDefineAttribute(SqlDefineTypes.MySql);
        Console.WriteLine($"   枚举构造: SqlDefineTypes.MySql -> {enumAttr.DialectType}");
        
        // 验证结果一致
        Console.WriteLine($"   结果一致: {stringAttr.DialectType == enumAttr.DialectType}");
        
        Console.WriteLine();
    }

    static void DemonstrateRealWorldUsage()
    {
        Console.WriteLine("🌍 实际使用场景演示:");
        
        // 类级别枚举特性
        var mySqlRepo = typeof(MySqlUserRepository);
        var classAttrs = mySqlRepo.GetCustomAttributes(typeof(SqlDefineAttribute), false);
        if (classAttrs.Length > 0)
        {
            var classAttr = (SqlDefineAttribute)classAttrs[0];
            Console.WriteLine($"   类级别特性: {mySqlRepo.Name} -> {classAttr.DialectType}");
        }
        
        // 方法级别枚举特性
        var method = mySqlRepo.GetMethod(nameof(MySqlUserRepository.GetArchivedUsers))!;
        var methodAttrs = method.GetCustomAttributes(typeof(SqlDefineAttribute), false);
        if (methodAttrs.Length > 0)
        {
            var methodAttr = (SqlDefineAttribute)methodAttrs[0];
            Console.WriteLine($"   方法级别特性: {method.Name} -> {methodAttr.DialectType}");
        }
        
        // 混合使用演示
        var pgRepo = typeof(PostgreSqlUserRepository);
        var pgClassAttrs = pgRepo.GetCustomAttributes(typeof(SqlDefineAttribute), false);
        if (pgClassAttrs.Length > 0)
        {
            var pgClassAttr = (SqlDefineAttribute)pgClassAttrs[0];
            Console.WriteLine($"   混合使用 - 类(字符串): {pgRepo.Name} -> {pgClassAttr.DialectName} ({pgClassAttr.DialectType})");
        }
        
        var pgMethod = pgRepo.GetMethod(nameof(PostgreSqlUserRepository.GetUsersByRole))!;
        var pgMethodAttrs = pgMethod.GetCustomAttributes(typeof(SqlDefineAttribute), false);
        if (pgMethodAttrs.Length > 0)
        {
            var pgMethodAttr = (SqlDefineAttribute)pgMethodAttrs[0];
            Console.WriteLine($"   混合使用 - 方法(枚举): {pgMethod.Name} -> {pgMethodAttr.DialectType}");
        }
    }
}

// 演示用的仓储类
[SqlDefine(SqlDefineTypes.MySql)]
public class MySqlUserRepository
{
    public void GetActiveUsers() { }
    
    [SqlDefine(SqlDefineTypes.SqlServer)]
    public void GetArchivedUsers() { }
}

[SqlDefine("PostgreSql")]
public class PostgreSqlUserRepository
{
    [SqlDefine(SqlDefineTypes.Oracle)]
    public void GetUsersByRole() { }
}
