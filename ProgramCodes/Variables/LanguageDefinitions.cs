using System.Collections.Generic;

namespace LexorInterpreter.ProgramCodes
{
    public enum DataType
    {
        INT, CHAR, BOOL, FLOAT, STRING,
        INT_ARR, CHAR_ARR, BOOL_ARR, FLOAT_ARR, STRING_ARR,
        INT_STACK, CHAR_STACK, BOOL_STACK, FLOAT_STACK, STRING_STACK
    }

    internal enum TokenKind
    {
        EOF,
        IDENT, INT_LITERAL, FLOAT_LITERAL, CHAR_LITERAL, BOOL_LITERAL, STRING_LITERAL,
        LPAREN, RPAREN, LBRACKET, RBRACKET,
        PLUS, MINUS, STAR, SLASH, PERCENT,
        GT, LT, GTE, LTE, EQEQ, NEQ,
        AND, OR, NOT
    }

    public static class TypeHelper
    {
        public static bool IsArrayType(DataType t) => t is DataType.INT_ARR or DataType.CHAR_ARR or DataType.BOOL_ARR or DataType.FLOAT_ARR or DataType.STRING_ARR;

        public static bool IsStackType(DataType t) => t is DataType.INT_STACK or DataType.CHAR_STACK or DataType.BOOL_STACK or DataType.FLOAT_STACK or DataType.STRING_STACK;

        public static DataType ElementType(DataType containerType) => containerType switch
        {
            DataType.INT_ARR or DataType.INT_STACK => DataType.INT,
            DataType.CHAR_ARR or DataType.CHAR_STACK => DataType.CHAR,
            DataType.BOOL_ARR or DataType.BOOL_STACK => DataType.BOOL,
            DataType.FLOAT_ARR or DataType.FLOAT_STACK => DataType.FLOAT,
            DataType.STRING_ARR or DataType.STRING_STACK => DataType.STRING,
            _ => throw new InvalidOperationException($"Not a container type: {containerType}")
        };
    }

    public static class LanguageDefinitions
    {
        public static readonly HashSet<string> ReservedWords = new()
        {
            "SCRIPT", "AREA", "START", "END", "DECLARE",
            "PRINT", "SCAN", "INT", "CHAR", "BOOL", "FLOAT", "STRING",
            "INT_ARR", "CHAR_ARR", "BOOL_ARR", "FLOAT_ARR", "STRING_ARR",
            "INT_STACK", "CHAR_STACK", "BOOL_STACK", "FLOAT_STACK", "STRING_STACK",
            "TRUE", "FALSE", "AND", "OR", "NOT",
            "IF", "ELSE", "FOR", "REPEAT", "WHEN",
            "PUSH", "POP", "TOP"
        };
    }
}
