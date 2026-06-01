// Parses and evaluates the token stream for LEXOR expressions.
// This file contains the recursive-descent parser and evaluation helpers.

using System.Globalization;

namespace LexorInterpreter.ProgramCodes
{
    internal sealed class Parser
    {
        private readonly List<Token> _tokens;
        private readonly int _lineNumber;
        private readonly Dictionary<string, Variable> _symbols;
        private int _pos;

        public Parser(List<Token> tokens, int lineNumber, Dictionary<string, Variable> symbols)
        {
            _tokens = tokens;
            _lineNumber = lineNumber;
            _symbols = symbols;
        }

        public bool IsAtEnd => Peek().Kind == TokenKind.EOF;
        public string PeekLexeme() => Peek().Lexeme;

        // OR is the lowest precedence operator in the spec list.
        public (object? value, DataType type, string? error) ParseExpression() => ParseOr();

        private (object? value, DataType type, string? error) ParseOr()
        {
            var left = ParseAnd();
            if (left.error != null) return left;

            while (Match(TokenKind.OR))
            {
                var right = ParseAnd();
                if (right.error != null) return right;
                var merged = ExpressionOperations.ApplyLogical(_lineNumber, "OR", left, right);
                if (merged.error != null) return merged;
                left = merged;
            }
            return left;
        }

        private (object? value, DataType type, string? error) ParseAnd()
        {
            var left = ParseComparison();
            if (left.error != null) return left;

            while (Match(TokenKind.AND))
            {
                var right = ParseComparison();
                if (right.error != null) return right;
                var merged = ExpressionOperations.ApplyLogical(_lineNumber, "AND", left, right);
                if (merged.error != null) return merged;
                left = merged;
            }
            return left;
        }

        private (object? value, DataType type, string? error) ParseComparison()
        {
            var left = ParseAddSub();
            if (left.error != null) return left;

            while (true)
            {
                TokenKind op = Peek().Kind;
                if (op is not (TokenKind.GT or TokenKind.LT or TokenKind.GTE or TokenKind.LTE or TokenKind.EQEQ or TokenKind.NEQ))
                    break;

                Advance(); // Consume operator.
                var right = ParseAddSub();
                if (right.error != null) return right;

                var applied = ExpressionOperations.ApplyComparison(_lineNumber, op, left, right);
                if (applied.error != null) return applied;
                left = applied;
            }
            return left;
        }

        private (object? value, DataType type, string? error) ParseAddSub()
        {
            var left = ParseMulDivMod();
            if (left.error != null) return left;

            while (true)
            {
                TokenKind op = Peek().Kind;
                if (op is not (TokenKind.PLUS or TokenKind.MINUS))
                    break;

                Advance();
                var right = ParseMulDivMod();
                if (right.error != null) return right;

                var applied = ExpressionOperations.ApplyArithmetic(_lineNumber, op, left, right);
                if (applied.error != null) return applied;
                left = applied;
            }

            return left;
        }

        private (object? value, DataType type, string? error) ParseMulDivMod()
        {
            var left = ParseUnary();
            if (left.error != null) return left;

            while (true)
            {
                TokenKind op = Peek().Kind;
                if (op is not (TokenKind.STAR or TokenKind.SLASH or TokenKind.PERCENT))
                    break;

                Advance();
                var right = ParseUnary();
                if (right.error != null) return right;

                var applied = ExpressionOperations.ApplyArithmetic(_lineNumber, op, left, right);
                if (applied.error != null) return applied;
                left = applied;
            }

            return left;
        }

        private (object? value, DataType type, string? error) ParseUnary()
        {
            if (Match(TokenKind.NOT))
            {
                var operand = ParseUnary();
                if (operand.error != null) return operand;
                if (operand.type != DataType.BOOL)
                    return (null, DataType.BOOL, $"Line {_lineNumber}: NOT expects a BOOL expression.");
                return (!(bool)operand.value!, DataType.BOOL, null);
            }

            if (Match(TokenKind.PLUS))
            {
                var operand = ParseUnary();
                if (operand.error != null) return operand;
                if (!ExpressionOperations.IsNumeric(operand.type))
                    return (null, DataType.INT, $"Line {_lineNumber}: Unary + expects a numeric expression.");
                return operand;
            }

            if (Match(TokenKind.MINUS))
            {
                var operand = ParseUnary();
                if (operand.error != null) return operand;
                if (!ExpressionOperations.IsNumeric(operand.type))
                    return (null, DataType.INT, $"Line {_lineNumber}: Unary - expects a numeric expression.");
                return operand.type == DataType.FLOAT
                    ? (-(float)operand.value!, DataType.FLOAT, null)
                    : (-(int)operand.value!, DataType.INT, null);
            }

            return ParsePrimary();
        }

        private (object? value, DataType type, string? error) ParsePrimary()
        {
            if (Match(TokenKind.LPAREN))
            {
                var inner = ParseExpression();
                if (inner.error != null) return inner;
                if (!Match(TokenKind.RPAREN))
                    return (null, DataType.INT, $"Line {_lineNumber}: Missing ')'.");
                return inner;
            }

            if (Match(TokenKind.INT_LITERAL, out var intTok))
            {
                if (!int.TryParse(intTok!.Lexeme, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intVal))
                    return (null, DataType.INT, $"Line {_lineNumber}: INT literal out of range.");
                return (intVal, DataType.INT, null);
            }

            if (Match(TokenKind.FLOAT_LITERAL, out var floatTok))
            {
                if (!float.TryParse(floatTok!.Lexeme, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatVal))
                    return (null, DataType.FLOAT, $"Line {_lineNumber}: FLOAT literal out of range.");
                if (float.IsInfinity(floatVal) || float.IsNaN(floatVal))
                    return (null, DataType.FLOAT, $"Line {_lineNumber}: FLOAT literal out of range.");
                if (floatVal == 0f && floatTok.Lexeme.StartsWith("-", StringComparison.Ordinal))
                    return (null, DataType.FLOAT, $"Line {_lineNumber}: -0.0 is not allowed.");
                return (floatVal, DataType.FLOAT, null);
            }

            if (Match(TokenKind.CHAR_LITERAL, out var charTok))
                return (charTok!.Lexeme[0], DataType.CHAR, null);

            if (Match(TokenKind.BOOL_LITERAL, out var boolTok))
            {
                if (boolTok!.Lexeme == "TRUE") return (true, DataType.BOOL, null);
                if (boolTok.Lexeme == "FALSE") return (false, DataType.BOOL, null);
                return (null, DataType.BOOL, $"Line {_lineNumber}: BOOL literals must be uppercase TRUE/FALSE.");
            }

            if (Match(TokenKind.STRING_LITERAL, out var stringTok))
                return (stringTok!.Lexeme, DataType.STRING, null);

            if (Match(TokenKind.IDENT, out var identTok))
            {
                string name = identTok!.Lexeme;
                if (!_symbols.TryGetValue(name, out var variable))
                    return (null, DataType.INT, $"Line {_lineNumber}: Undefined variable '{name}'.");
                if (!variable.IsInitialized)
                    return (null, DataType.INT, $"Line {_lineNumber}: Variable '{name}' is uninitialized.");
                return (variable.Value, variable.DataType, null);
            }

            return (null, DataType.INT, $"Line {_lineNumber}: Unexpected token '{Peek().Lexeme}'.");
        }

        private Token Peek() => _tokens[_pos];
        private Token Advance() => _tokens[_pos++];

        private bool Match(TokenKind kind) => Match(kind, out _);

        private bool Match(TokenKind kind, out Token? token)
        {
            token = null;
            if (Peek().Kind != kind) return false;
            token = Advance();
            return true;
        }
    }
}

