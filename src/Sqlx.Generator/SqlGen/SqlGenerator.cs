// -----------------------------------------------------------------------
// <copyright file="SqlGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.Generator.Core;

namespace Sqlx.SqlGen
{
    internal sealed class SqlGenerator
    {
        public string Generate(SqlDefine def, int type, GenerateContext ctx)
        {
            return type switch
            {
                Constants.SqlExecuteTypeValues.Select => GenerateSelect(def, (SelectGenerateContext)ctx),
                Constants.SqlExecuteTypeValues.Insert => GenerateInsert(def, (InsertGenerateContext)ctx),
                Constants.SqlExecuteTypeValues.Update => GenerateUpdate(def, (UpdateGenerateContext)ctx),
                Constants.SqlExecuteTypeValues.Delete => GenerateDelete(def, (DeleteGenerateContext)ctx),
                _ => string.Empty
            };
        }

        private string GenerateSelect(SqlDefine def, SelectGenerateContext ctx) => $"SELECT {ctx.GetColumnNames()} FROM {def.WrapColumn(ctx.TableName)}";

        private string GenerateInsert(SqlDefine def, InsertGenerateContext ctx)
        {
            var wrappedTable = def.WrapColumn(ctx.TableName);
            var columns = ctx.GetColumnNames();
            return ctx.Entry.IsList
                ? $"INSERT INTO {wrappedTable}({columns}) VALUES {{{{VALUES_PLACEHOLDER}}}}"
                : $"INSERT INTO {wrappedTable}({columns}) VALUES ({ctx.GetParamterNames(def.ParameterPrefix)})";
        }

        private string GenerateUpdate(SqlDefine def, UpdateGenerateContext ctx) => $"UPDATE {def.WrapColumn(ctx.TableName)} SET {ctx.GetUpdateSet(def.ParameterPrefix)} WHERE {{0}}";

        private string GenerateDelete(SqlDefine def, DeleteGenerateContext ctx) => $"DELETE FROM {def.WrapColumn(ctx.TableName)} WHERE {{0}}";
    }
}
