// -----------------------------------------------------------------------
// <copyright file="SqlGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.SqlGen
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.CodeAnalysis;

    internal sealed class SqlGenerator
    {
        public string Generate(SqlDefine def, SqlExecuteTypes type, GenerateContext ctx)
        {
            return type switch
            {
                SqlExecuteTypes.Select => GenerateSelect(def, (SelectGenerateContext)ctx),
                SqlExecuteTypes.Insert => GenerateInsert(def, (InsertGenerateContext)ctx),
                SqlExecuteTypes.Update => GenerateUpdate(def, (UpdateGenerateContext)ctx),
                SqlExecuteTypes.Delete => GenerateDelete(def, (DeleteGenerateContext)ctx),
                _ => string.Empty
            };
        }

        private string GenerateSelect(SqlDefine def, SelectGenerateContext ctx)
            => $"SELECT {ctx.GetColumnNames()} FROM {def.WrapColumn(ctx.TableName)}";

        private string GenerateInsert(SqlDefine def, InsertGenerateContext ctx)
        {
            var tableName = def.WrapColumn(ctx.TableName);
            var columns = ctx.GetColumnNames();
            
            // Single INSERT: generate complete SQL with parameter placeholders
            if (!ctx.Entry.IsList)
            {
                return $"INSERT INTO {tableName}({columns}) VALUES ({ctx.GetParamterNames(def.ParamterPrefx)})";
            }

            // Batch INSERT: generate template for dynamic VALUES clause generation
            return $"INSERT INTO {tableName}({columns}) VALUES {{{{VALUES_PLACEHOLDER}}}}";
        }

        private string GenerateUpdate(SqlDefine def, UpdateGenerateContext ctx)
            => $"UPDATE {def.WrapColumn(ctx.TableName)} SET {ctx.GetUpdateSet(def.ParamterPrefx)} WHERE {{0}}";

        private string GenerateDelete(SqlDefine def, DeleteGenerateContext ctx)
            => $"DELETE FROM {def.WrapColumn(ctx.TableName)} WHERE {{0}}";
    }
}
