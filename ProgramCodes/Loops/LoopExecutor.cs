// Executes a parsed LoopBlock.
//
// FOR loop:
//   1. Execute the init statement once.
//   2. Evaluate the condition; if false, stop.
//   3. Execute the body via the executeLines callback (supports nesting).
//   4. Execute the update statement.
//   5. Go to step 2.
//
// REPEAT WHEN loop (while-loop semantics):
//   1. Evaluate the condition; if false, stop.
//   2. Execute the body via the executeLines callback (supports nesting).
//   3. Go to step 1.
//
// The executeLines callback is the same delegate used by the Interpreter's
// ExecuteLines method, so arbitrarily nested IF/FOR/REPEAT blocks work
// automatically without any extra logic here.

namespace LexorInterpreter.ProgramCodes
{
    public static class LoopExecutor
    {
        // Safety cap – prevents runaway infinite loops from hanging the interpreter.
        private const int MaxIterations = 100_000;

        /// <summary>
        /// Executes <paramref name="block"/> using
        /// <paramref name="executeStatement"/> for the init/update single statements and
        /// <paramref name="executeLines"/> for the body.
        /// </summary>
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

        // -------------------------------------------------------------------
        // FOR
        // -------------------------------------------------------------------

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
                    return $"Line {lineNum}: FOR loop exceeded {MaxIterations} iterations. Possible infinite loop.";

                // 2. Condition check.
                var (condValue, condType, condErr) = ExpressionEvaluator.Evaluate(
                    block.Condition!, lineNum, symbolTable);

                if (condErr != null) return condErr;

                if (condType != DataType.BOOL)
                    return $"Line {lineNum}: FOR loop condition must evaluate to BOOL, got {condType}.";

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

        // -------------------------------------------------------------------
        // REPEAT WHEN  (pre-test while loop)
        // -------------------------------------------------------------------

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

                // 1. Condition check.
                var (condValue, condType, condErr) = ExpressionEvaluator.Evaluate(
                    block.Condition!, lineNum, symbolTable);

                if (condErr != null) return condErr;

                if (condType != DataType.BOOL)
                    return $"Line {lineNum}: REPEAT WHEN condition must evaluate to BOOL, got {condType}.";

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
