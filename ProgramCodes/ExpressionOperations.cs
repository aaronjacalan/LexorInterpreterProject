// Shared evaluation helpers for LEXOR expressions.
// Separated from the parser to keep files small and focused.

namespace LexorInterpreter.ProgramCodes
{
    internal static class ExpressionOperations
    {
        internal static (object? value, DataType type, string? error) ApplyLogical(
            int lineNumber,
            string op,
            (object? value, DataType type, string? error) left,
            (object? value, DataType type, string? error) right)
        {
            if (left.type != DataType.BOOL || right.type != DataType.BOOL)
                return (null, DataType.BOOL, $"Line {lineNumber}: {op} expects two BOOL expressions.");

            bool a = (bool)left.value!;
            bool b = (bool)right.value!;
            return op == "AND" ? (a && b, DataType.BOOL, null) : (a || b, DataType.BOOL, null);
        }

        internal static (object? value, DataType type, string? error) ApplyComparison(
            int lineNumber,
            TokenKind op,
            (object? value, DataType type, string? error) left,
            (object? value, DataType type, string? error) right)
        {
            // For ordering comparisons, restrict to numeric types.
            if (op is TokenKind.GT or TokenKind.LT or TokenKind.GTE or TokenKind.LTE)
            {
                if (!IsNumeric(left.type) || !IsNumeric(right.type))
                    return (null, DataType.BOOL, $"Line {lineNumber}: Comparison expects numeric expressions.");

                float a = AsFloat(left);
                float b = AsFloat(right);
                bool res = op switch
                {
                    TokenKind.GT => a > b,
                    TokenKind.LT => a < b,
                    TokenKind.GTE => a >= b,
                    TokenKind.LTE => a <= b,
                    _ => false
                };
                return (res, DataType.BOOL, null);
            }

            // Equality / inequality allow numeric, CHAR, BOOL (same type), and numeric cross-type.
            if (op is TokenKind.EQEQ or TokenKind.NEQ)
            {
                bool eq;
                if (IsNumeric(left.type) && IsNumeric(right.type))
                {
                    eq = Math.Abs(AsFloat(left) - AsFloat(right)) < 0.000001f;
                }
                else if (left.type == right.type)
                {
                    eq = Equals(left.value, right.value);
                }
                else
                {
                    return (null, DataType.BOOL, $"Line {lineNumber}: Cannot compare {left.type} with {right.type}.");
                }

                return (op == TokenKind.EQEQ ? eq : !eq, DataType.BOOL, null);
            }

            return (null, DataType.BOOL, $"Line {lineNumber}: Unsupported comparison operator.");
        }

        internal static (object? value, DataType type, string? error) ApplyArithmetic(
            int lineNumber,
            TokenKind op,
            (object? value, DataType type, string? error) left,
            (object? value, DataType type, string? error) right)
        {
            if (!IsNumeric(left.type) || !IsNumeric(right.type))
                return (null, DataType.INT, $"Line {lineNumber}: Arithmetic expects numeric expressions.");

            bool floatMode = left.type == DataType.FLOAT || right.type == DataType.FLOAT;
            if (floatMode)
            {
                float a = AsFloat(left);
                float b = AsFloat(right);
                float res = op switch
                {
                    TokenKind.PLUS => a + b,
                    TokenKind.MINUS => a - b,
                    TokenKind.STAR => a * b,
                    TokenKind.SLASH => a / b,
                    TokenKind.PERCENT => a % b,
                    _ => 0f
                };
                return (res, DataType.FLOAT, null);
            }

            int ai = (int)left.value!;
            int bi = (int)right.value!;
            int ires = op switch
            {
                TokenKind.PLUS => ai + bi,
                TokenKind.MINUS => ai - bi,
                TokenKind.STAR => ai * bi,
                TokenKind.SLASH => ai / bi,
                TokenKind.PERCENT => ai % bi,
                _ => 0
            };
            return (ires, DataType.INT, null);
        }

        internal static bool IsNumeric(DataType t) => t == DataType.INT || t == DataType.FLOAT;

        internal static float AsFloat((object? value, DataType type, string? error) v)
            => v.type == DataType.FLOAT ? (float)v.value! : (int)v.value!;
    }
}

