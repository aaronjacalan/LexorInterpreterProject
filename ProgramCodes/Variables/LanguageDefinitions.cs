namespace LexorInterpreter.ProgramCodes
{
    public enum DataType
    {
        INT, CHAR, BOOL, FLOAT, STRING
    }

    internal enum TokenKind
    {
        // Special.
        EOF,

        // Identifiers + literals.
        IDENT, INT_LITERAL, FLOAT_LITERAL, CHAR_LITERAL, BOOL_LITERAL, STRING_LITERAL,

        // Grouping.
        LPAREN, RPAREN,

        // Arithmetic.
        PLUS, MINUS, STAR, SLASH, PERCENT,

        // Comparisons.
        GT, LT, GTE, LTE, EQEQ, NEQ,

        // Gates.
        AND, OR, NOT
    }

    public static class LanguageDefinitions
    {
        // Every reserved word in the LEXOR language.
        public static readonly HashSet<string> ReservedWords = new()
        {
            "SCRIPT", "AREA", "START", "END", "DECLARE",
            "PRINT", "SCAN", "INT", "CHAR", "BOOL", "FLOAT", "STRING",
            "TRUE", "FALSE", "AND", "OR", "NOT",
            "IF", "ELSE", "FOR", "REPEAT", "WHEN"
        };
    }
}
