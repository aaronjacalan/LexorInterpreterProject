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
                    return (null, DataType.BOOL, $"NOT expects a BOOL expression.");
                return (!(bool)operand.value!, DataType.BOOL, null);
            }

            if (Match(TokenKind.PLUS))
            {
                var operand = ParseUnary();
                if (operand.error != null) return operand;
                if (!ExpressionOperations.IsNumeric(operand.type))
                    return (null, DataType.INT, $"Unary + expects a numeric expression.");
                return operand;
            }

            if (Match(TokenKind.MINUS))
            {
                var operand = ParseUnary();
                if (operand.error != null) return operand;
                if (!ExpressionOperations.IsNumeric(operand.type))
                    return (null, DataType.INT, $"Unary - expects a numeric expression.");
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
                    return (null, DataType.INT, $"Missing ')'.");
                return inner;
            }

            if (Match(TokenKind.INT_LITERAL, out var intTok))
            {
                if (!int.TryParse(intTok!.Lexeme, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intVal))
                    return (null, DataType.INT, $"INT literal out of range.");
                return (intVal, DataType.INT, null);
            }

            if (Match(TokenKind.FLOAT_LITERAL, out var floatTok))
            {
                if (!float.TryParse(floatTok!.Lexeme, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatVal))
                    return (null, DataType.FLOAT, $"FLOAT literal out of range.");
                if (float.IsInfinity(floatVal) || float.IsNaN(floatVal))
                    return (null, DataType.FLOAT, $"FLOAT literal out of range.");
                if (floatVal == 0f && floatTok.Lexeme.StartsWith("-", StringComparison.Ordinal))
                    return (null, DataType.FLOAT, $"-0.0 is not allowed.");
                return (floatVal, DataType.FLOAT, null);
            }

            if (Match(TokenKind.CHAR_LITERAL, out var charTok))
                return (charTok!.Lexeme[0], DataType.CHAR, null);

            if (Match(TokenKind.BOOL_LITERAL, out var boolTok))
            {
                if (boolTok!.Lexeme == "TRUE") return (true, DataType.BOOL, null);
                if (boolTok.Lexeme == "FALSE") return (false, DataType.BOOL, null);
                return (null, DataType.BOOL, $"BOOL literals must be uppercase TRUE/FALSE.");
            }

            if (Match(TokenKind.STRING_LITERAL, out var stringTok))
                return (stringTok!.Lexeme, DataType.STRING, null);

            if (Match(TokenKind.IDENT, out var identTok))
            {
                string name = identTok!.Lexeme;

                // TOP(stack_name) — returns top element without removing.
                if (string.Equals(name, "TOP", System.StringComparison.OrdinalIgnoreCase) && Peek().Kind == TokenKind.LPAREN)
                {
                    Advance();
                    if (!Match(TokenKind.IDENT, out var stackNameTok))
                        return (null, DataType.INT, $"TOP expects a stack variable name.");
                    if (!Match(TokenKind.RPAREN))
                        return (null, DataType.INT, $"Missing ')' after TOP argument.");

                    string stackName = stackNameTok!.Lexeme;
                    if (!_symbols.TryGetValue(stackName, out var stackVar))
                        return (null, DataType.INT, $"Undefined stack '{stackName}'.");
                    if (!TypeHelper.IsStackType(stackVar.DataType))
                        return (null, DataType.INT, $"'{stackName}' is not a stack.");

                    var stack = (System.Collections.Generic.Stack<object>)stackVar.Value!;
                    if (stack.Count == 0)
                        return (null, DataType.INT, $"Stack '{stackName}' is empty.");

                    DataType elemType = TypeHelper.ElementType(stackVar.DataType);
                    return (stack.Peek(), elemType, null);
                }

                // Array indexing: ident [ expr ]
                if (Match(TokenKind.LBRACKET))
                {
                    if (!_symbols.TryGetValue(name, out var arrVar))
                        return (null, DataType.INT, $"Undefined array '{name}'.");
                    if (!TypeHelper.IsArrayType(arrVar.DataType))
                        return (null, DataType.INT, $"'{name}' is not an array.");
                    if (!arrVar.IsInitialized)
                        return (null, DataType.INT, $"Array '{name}' is uninitialized.");

                    var indexResult = ParseExpression();
                    if (indexResult.error != null) return indexResult;
                    if (indexResult.type != DataType.INT)
                        return (null, DataType.INT, $"Array index must be INT, got {indexResult.type}.");

                    if (!Match(TokenKind.RBRACKET))
                        return (null, DataType.INT, $"Missing ']' after array index.");

                    int idx = (int)indexResult.value!;
                    var arr = (object[])arrVar.Value!;
                    if (idx < 0 || idx >= arr.Length)
                        return (null, DataType.INT, $"Array index {idx} out of bounds (size {arr.Length}).");

                    object? elem = arr[idx];
                    DataType elemType = TypeHelper.ElementType(arrVar.DataType);

                    if (elem == null && elemType != DataType.STRING)
                        return (null, elemType, $"Array element '{name}[{idx}]' is uninitialized.");

                    return (elem, elemType, null);
                }

                if (!_symbols.TryGetValue(name, out var variable))
                    return (null, DataType.INT, $"Undefined variable '{name}'.");
                if (!variable.IsInitialized)
                    return (null, DataType.INT, $"Variable '{name}' is uninitialized.");
                return (variable.Value, variable.DataType, null);
            }

            return (null, DataType.INT, $"Unexpected token '{Peek().Lexeme}'.");
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

