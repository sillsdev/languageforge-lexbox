﻿using System.Data.SQLite;
using System.Linq.Expressions;
using LinqToDB;
using SIL.WritingSystems;

namespace LcmCrdt.Data;

public static class SqlSortingExtensions
{
    public const string CollateUnicodeNoCase = "NOCASE_UNICODE";

    [ExpressionMethod(nameof(CollateUnicodeExpression))]
    internal static string CollateUnicode(this string value, WritingSystem ws)
    {
        //could optionally just return the value here, but it would work differently than sql
        throw new InvalidOperationException("CollateUnicode is a LinqToDB only API.");
    }

    private static Expression<Func<string, WritingSystem, string>> CollateUnicodeExpression()
    {
        //todo maybe in the future we use a custom collation based on the writing system
        return (s, ws) => s.Collate(CollationName(ws));
    }

    internal static string CollationName(WritingSystem ws)
    {
        //don't use ':' in the name, it won't work
        return $"NOCASE_WS_{ws.WsId}";
    }
}