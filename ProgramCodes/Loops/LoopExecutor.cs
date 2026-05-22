// Executes a parsed LoopBlock.
// FOR:   init → check condition → body → update → repeat
// REPEAT: check condition → body → repeat

namespace LexorInterpreter.ProgramCodes
{
    public static class LoopExecutor
    {
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

            // Init.
            if (!string.IsNullOrWhiteSpace(block.InitStatement))
            {
                string? initErr = executeStatement(block.InitStatement!, lineNum);
                if (initErr != null) return initErr;
            }

            int iterations = 0;
            while (true)
            {
                if (++iterations > MaxIterations)
                    return $"Line {lineNum}: FOR loop exceeded {MaxIterations} iterations. Possible infinite loop.";

                // Condition.
                var (condValue, condType, condErr) = ExpressionEvaluator.Evaluate(
                    block.Condition!, lineNum, symbolTable);

                if (condErr != null) return condErr;

                if (condType != DataType.BOOL)
                    return $"Line {lineNum}: FOR loop condition must evaluate to BOOL, got {condType}.";

                if (!(bool)condValue!)
                    break;

                // Body.
                string? bodyErr = executeLines(block.Body);
                if (bodyErr != null) return bodyErr;

                // Update.
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
                    return $"Line {lineNum}: REPEAT loop exceeded {MaxIterations} iterations. Possible infinite loop.";

                // Condition.
                var (condValue, condType, condErr) = ExpressionEvaluator.Evaluate(
                    block.Condition!, lineNum, symbolTable);

                if (condErr != null) return condErr;

                if (condType != DataType.BOOL)
                    return $"Line {lineNum}: REPEAT WHEN condition must evaluate to BOOL, got {condType}.";

                if (!(bool)condValue!)
                    break;

                // Body.
                string? bodyErr = executeLines(block.Body);
                if (bodyErr != null) return bodyErr;
            }

            return null;
        }
    }
}
