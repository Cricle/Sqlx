// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlBase.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sqlx.Expressions;

namespace Sqlx
{
    /// <summary>
    /// Abstract base class for ExpressionToSql with common expression parsing and database dialect adaptation.
    /// </summary>
    public abstract class ExpressionToSqlBase
    {
        internal readonly SqlDialect _dialect;
        internal readonly List<string> _whereConditions = new();
        internal readonly List<string> _orderByExpressions = new();
        internal readonly List<string> _groupByExpressions = new();
        internal readonly List<string> _havingConditions = new();
        internal readonly Dictionary<string, object?> _parameters = new();
        internal int? _take;
        internal int? _skip;
        internal string? _tableName;

        /// <summary>Whether to use parameterized query mode.</summary>
        protected bool _parameterized;

        /// <summary>The expression parser instance.</summary>
        private ExpressionParser? _parser;

        /// <summary>Initializes with SQL dialect and entity type.</summary>
        protected ExpressionToSqlBase(SqlDialect dialect, Type entityType)
        {
            _dialect = dialect;
            _tableName = entityType.Name;
        }

        /// <summary>Gets the database type string.</summary>
        protected string DatabaseType => _dialect.DatabaseType;

        /// <summary>Gets or creates the expression parser.</summary>
        private ExpressionParser Parser => _parser ??= new ExpressionParser(_dialect, _parameters, _parameterized);

        #region Public Methods

        /// <summary>Adds GROUP BY column.</summary>
        public virtual ExpressionToSqlBase AddGroupBy(string columnName)
        {
            if (!string.IsNullOrEmpty(columnName) && !_groupByExpressions.Contains(columnName))
            {
                _groupByExpressions.Add(columnName);
            }

            return this;
        }

        /// <summary>Sets table name.</summary>
        public void SetTableName(string tableName) => _tableName = tableName;

        /// <summary>Gets WHERE clause without WHERE keyword.</summary>
        public virtual string ToWhereClause()
        {
            if (_whereConditions.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" AND ", _whereConditions.Select(ExpressionHelper.RemoveOuterParentheses));
        }

        /// <summary>Gets parameters dictionary for command binding.</summary>
        public virtual Dictionary<string, object?> GetParameters() => new(_parameters);

        /// <summary>Converts to SQL string.</summary>
        public abstract string ToSql();

        /// <summary>Converts to SQL template string.</summary>
        public abstract string ToTemplate();

        #endregion

        #region Internal Methods

        internal void CopyWhereConditions(List<string> conditions) => _whereConditions.AddRange(conditions);

        internal void CopyHavingConditions(List<string> conditions) => _havingConditions.AddRange(conditions);

        internal void AddHavingCondition(string condition) => _havingConditions.Add(condition);

        #endregion

        #region Expression Parsing (Delegated to ExpressionParser)

        /// <summary>Parse expression to SQL.</summary>
        protected string ParseExpression(Expression expression, bool treatBoolAsComparison = true)
            => Parser.Parse(expression, treatBoolAsComparison);

        /// <summary>Parses expression as raw value.</summary>
        protected string ParseExpressionRaw(Expression expression) => Parser.ParseRaw(expression);

        /// <summary>Get column name from expression.</summary>
        protected string GetColumnName(Expression expression) => Parser.GetColumnName(expression);

        /// <summary>Extract column names from expression.</summary>
        protected List<string> ExtractColumns(Expression expression) => Parser.ExtractColumns(expression);

        #endregion

        #region Value Formatting (Delegated to ValueFormatter)

        /// <summary>Formats constant value.</summary>
        protected string FormatConstantValue(object? value)
            => _parameterized ? CreateParameter(value) : ValueFormatter.FormatAsLiteral(_dialect, value);

        /// <summary>Creates parameter.</summary>
        protected virtual string CreateParameter(object? value) => Parser.CreateParameter(value);

        #endregion

        #region Helper Methods (Delegated to ExpressionHelper)

        /// <summary>Removes outer parentheses from condition.</summary>
        protected static string RemoveOuterParentheses(string condition) => ExpressionHelper.RemoveOuterParentheses(condition);

        /// <summary>Gets binary operator SQL string based on database dialect.</summary>
        protected string GetBinaryOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Add => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.Multiply => "*",
                ExpressionType.Divide => "/",
                ExpressionType.Modulo => DatabaseType == "Oracle" ? "MOD" : "%",
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => DatabaseType == "Oracle" ? "!=" : "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                ExpressionType.Coalesce => "COALESCE",
                _ => throw new NotSupportedException($"Binary operator {nodeType} is not supported")
            };
        }

        /// <summary>Evaluates expression to get runtime value.</summary>
        internal static object? EvaluateExpression(Expression expression) => ExpressionHelper.EvaluateExpression(expression);

        #endregion
    }
}
