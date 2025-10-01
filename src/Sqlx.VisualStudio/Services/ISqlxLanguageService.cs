using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlx.VisualStudio.Services
{
    /// <summary>
    /// Sqlx语言服务接口
    /// 负责分析C#代码中的Sqlx特性并提取相关信息
    /// </summary>
    public interface ISqlxLanguageService
    {
        /// <summary>
        /// 获取解决方案中所有带有Sqlx特性的方法信息
        /// </summary>
        /// <returns>Sqlx方法信息列表</returns>
        Task<IReadOnlyList<SqlxMethodInfo>> GetSqlxMethodsAsync();

        /// <summary>
        /// 获取指定文档中所有带有Sqlx特性的方法信息
        /// </summary>
        /// <param name="document">要分析的文档</param>
        /// <returns>Sqlx方法信息列表</returns>
        Task<IReadOnlyList<SqlxMethodInfo>> GetSqlxMethodsInDocumentAsync(Document document);

        /// <summary>
        /// 刷新语言服务缓存
        /// </summary>
        Task RefreshCacheAsync();

        /// <summary>
        /// 初始化服务
        /// </summary>
        void Initialize();
    }

    /// <summary>
    /// Sqlx方法信息
    /// </summary>
    public class SqlxMethodInfo
    {
        public SqlxMethodInfo(
            string methodName,
            string className,
            string namespaceName,
            string sqlQuery,
            string filePath,
            int lineNumber,
            int columnNumber,
            string methodSignature,
            IReadOnlyList<SqlxParameterInfo> parameters,
            SqlxAttributeType attributeType)
        {
            MethodName = methodName;
            ClassName = className;
            Namespace = namespaceName;
            SqlQuery = sqlQuery;
            FilePath = filePath;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
            MethodSignature = methodSignature;
            Parameters = parameters;
            AttributeType = attributeType;
        }

        public string MethodName { get; }
        public string ClassName { get; }
        public string Namespace { get; }
        public string SqlQuery { get; }
        public string FilePath { get; }
        public int LineNumber { get; }
        public int ColumnNumber { get; }
        public string MethodSignature { get; }
        public IReadOnlyList<SqlxParameterInfo> Parameters { get; }
        public SqlxAttributeType AttributeType { get; }
    }

    /// <summary>
    /// Sqlx方法参数信息
    /// </summary>
    public class SqlxParameterInfo
    {
        public SqlxParameterInfo(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public string Type { get; }
    }

    /// <summary>
    /// Sqlx特性类型枚举
    /// </summary>
    public enum SqlxAttributeType
    {
        /// <summary>
        /// [Sqlx] 特性
        /// </summary>
        Sqlx,

        /// <summary>
        /// [SqlTemplate] 特性
        /// </summary>
        SqlTemplate,

        /// <summary>
        /// [ExpressionToSql] 特性
        /// </summary>
        ExpressionToSql
    }
}
