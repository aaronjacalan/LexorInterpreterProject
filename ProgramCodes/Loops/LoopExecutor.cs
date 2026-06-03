// Executes a parsed LoopBlock.
// FOR:   init → check condition → body → update → repeat
// REPEAT: check condition → body → repeat

namespace LexorInterpreter.ProgramCodes
{
    public static class LoopExecutor
    {
        // Safety cap — prevents infinite loops from hanging the interpreter.
        private const int MaxIterations = 100_000;

        public static string? Execute(
            LoopBlock block,
            Dictionary<string, Variable> symbolTable,
            Func<List<(int LineNumber, string Content)>, string?> executeLines,
            Func<string, int, string?> executeStatement)
        {
            return block.Kind == LoopKind.For
                ? ExecuteFor(block, symbolTable, executeLines, executeStatement)
                : ExecuteRepeat(block, symbolTable, executeLines);
        }

        private static string? ExecuteFor(
            LoopBlock block,
            Dictionary<string, Variable> symbolTable,
            Func<List<(int LineNumber, string Content)>, string?> executeLines,
            Func<string, int, string?> executeStatement)
        {
            int lineNum = block.ConditionLine;

            // 1. Init.
            if (!string.IsNullOrWhiteSpace(block.InitStatement))
            {
                string? initErr = executeStatement(block.InitStatement!, lineNum);
                if (initErr != null) return initErr;
            }

            int iterations = 0;
            while (true)
            {
                if (++iterations > MaxIterations)
                    return $"FOR loop exceeded {MaxIterations} iterations. Possible infinite loop.";

                // 2. Condition — stop when false.
                var (condValue, condType, condErr) = ExpressionEvaluator.Evaluate(
                    block.Condition!, lineNum, symbolTable);

                if (condErr != null) return condErr;

                if (condType != DataType.BOOL)
                    return $"FOR loop condition must evaluate to BOOL, got {condType}.";

                if (!(bool)condValue!)
                    break;

                // 3. Body.
                string? bodyErr = executeLines(block.Body);
                if (bodyErr != null) return bodyErr;

                // 4. Update.
                if (!string.IsNullOrWhiteSpace(block.UpdateStatement))
                {
                    string? updErr = executeStatement(block.UpdateStatement!, lineNum);
                    if (updErr != null) return updErr;
                }
            }

            return null;
        }

        private static string? ExecuteRepeat(
            LoopBlock block,
            Dictionary<string, Variable> symbolTable,
            Func<List<(int LineNumber, string Content)>, string?> executeLines)
        {
            int lineNum = block.ConditionLine;
            int iterations = 0;

            while (true)
            {
                if (++iterations > MaxIterations)
                    return $"REPEAT loop exceeded {MaxIterations} iterations. Possible infinite loop.";

                // 1. Condition — stop when false.
                var (condValue, condType, condErr) = ExpressionEvaluator.Evaluate(
                    block.Condition!, lineNum, symbolTable);

                if (condErr != null) return condErr;

                if (condType != DataType.BOOL)
                    return $"REPEAT WHEN condition must evaluate to BOOL, got {condType}.";

                if (!(bool)condValue!)
                    break;

                // 2. Body.
                string? bodyErr = executeLines(block.Body);
                if (bodyErr != null) return bodyErr;
            }

            return null;
        }
    }
}
