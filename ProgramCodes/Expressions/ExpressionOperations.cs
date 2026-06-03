// Shared evaluation helpers for LEXOR expressions.

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
                return (null, DataType.BOOL, $"{op} expects two BOOL expressions.");

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
                    return (null, DataType.BOOL, $"Comparison expects numeric expressions.");

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

            // Equality/inequality allow numeric, CHAR, BOOL (same type), and numeric cross-type.
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
                    return (null, DataType.BOOL, $"Cannot compare {left.type} with {right.type}.");
                }

                return (op == TokenKind.EQEQ ? eq : !eq, DataType.BOOL, null);
            }

            return (null, DataType.BOOL, $"Unsupported comparison operator.");
        }

        internal static (object? value, DataType type, string? error) ApplyArithmetic(
            int lineNumber,
            TokenKind op,
            (object? value, DataType type, string? error) left,
            (object? value, DataType type, string? error) right)
        {
            if (!IsNumeric(left.type) || !IsNumeric(right.type))
                return (null, DataType.INT, $"Arithmetic expects numeric expressions.");

            bool floatMode = left.type == DataType.FLOAT || right.type == DataType.FLOAT;
            if (floatMode)
            {
                float a = AsFloat(left);
                float b = AsFloat(right);
                if ((op == TokenKind.SLASH || op == TokenKind.PERCENT) && b == 0f)
                    return (null, DataType.FLOAT, $"Division by zero.");

                float res = op switch
                {
                    TokenKind.PLUS => a + b,
                    TokenKind.MINUS => a - b,
                    TokenKind.STAR => a * b,
                    TokenKind.SLASH => a / b,
                    TokenKind.PERCENT => a % b,
                    _ => 0f
                };

                if (float.IsInfinity(res) || float.IsNaN(res))
                    return (null, DataType.FLOAT, $"FLOAT overflow or invalid value.");

                return (res, DataType.FLOAT, null);
            }

            int ai = (int)left.value!;
            int bi = (int)right.value!;
            if ((op == TokenKind.SLASH || op == TokenKind.PERCENT) && bi == 0)
                return (null, DataType.INT, $"Division by zero.");

            try
            {
                int ires = op switch
                {
                    TokenKind.PLUS => checked(ai + bi),
                    TokenKind.MINUS => checked(ai - bi),
                    TokenKind.STAR => checked(ai * bi),
                    TokenKind.SLASH => ai / bi,
                    TokenKind.PERCENT => ai % bi,
                    _ => 0
                };
                return (ires, DataType.INT, null);
            }
            catch (OverflowException)
            {
                return (null, DataType.INT, $"INT overflow.");
            }
        }

        internal static bool IsNumeric(DataType t) => t == DataType.INT || t == DataType.FLOAT;

        internal static float AsFloat((object? value, DataType type, string? error) v)
            => v.type == DataType.FLOAT ? (float)v.value! : (int)v.value!;
    }
}

