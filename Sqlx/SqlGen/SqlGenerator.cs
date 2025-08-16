// -----------------------------------------------------------------------
// <copyright file="SqlGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.SqlGen
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal sealed class SqlGenerator
    {
        public string Generate(SqlDefine def, SqlExecuteTypes type, GenerateContext ctx)
        {
            if (type == SqlExecuteTypes.Insert)
            {
                return GenerateInsert(def, (InsertGenerateContext)ctx);
            }

            return string.Empty;
        }

        private string GenerateInsert(SqlDefine def, InsertGenerateContext ctx)
        {
            var temp = $"INSERT INTO {def.WrapColumn(ctx.TableName)}({ctx.GetColumnNames()}) VALUES {ctx.GetParamterNames(def.ParamterPrefx)}";
            return temp;
        }
    }
}
