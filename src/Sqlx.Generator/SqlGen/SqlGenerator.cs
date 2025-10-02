// -----------------------------------------------------------------------
// <copyright file="SqlGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.Generator;

namespace Sqlx.SqlGen
{
    internal sealed class SqlGenerator
    {
        // Internal operation type constants
        private const int OpSelect = 0;
        private const int OpUpdate = 1;
        private const int OpInsert = 2;
        private const int OpDelete = 3;

        public string Generate(SqlDefine def, int type, GenerateContext ctx)
        {
            return type switch
            {
                OpSelect => GenerateSelect(def, (SelectGenerateContext)ctx),
                OpInsert => GenerateInsert(def, (InsertGenerateContext)ctx),
                OpUpdate => GenerateUpdate(def, (UpdateGenerateContext)ctx),
                OpDelete => GenerateDelete(def, (DeleteGenerateContext)ctx),
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
