// Evaluates LEXOR expressions used on the right-hand side of assignments.
// The implementation is split across:
// - ExpressionEvaluator (this file): public entry point
// - ExpressionTokenizer: turns raw text into tokens
// - ExpressionParser: recursive-descent evaluator (operator ordering per spec)

namespace LexorInterpreter.ProgramCodes
{
    public static class ExpressionEvaluator
    {
        public static (object? value, DataType type, string? error) Evaluate(
            string expr,
            int lineNumber,
            Dictionary<string, Variable> symbolTable)
        {
            var tokenizer = new Tokenizer(expr);
            var tokens = tokenizer.Tokenize(lineNumber);
            if (tokens.error != null) return (null, DataType.INT, tokens.error);

            var parser = new Parser(tokens.tokens!, lineNumber, symbolTable);
            var result = parser.ParseExpression();
            if (result.error != null) return (null, DataType.INT, result.error);

            if (!parser.IsAtEnd)
                return (null, DataType.INT, $"Unexpected token '{parser.PeekLexeme()}'.");

            return (result.value, result.type, null);
        }
    }
}

