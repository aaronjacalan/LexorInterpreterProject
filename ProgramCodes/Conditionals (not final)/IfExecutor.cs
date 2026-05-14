// Executes a parsed IfBlock:
// - Evaluates each branch condition in order
// - Runs the first branch whose condition is TRUE (or the ELSE branch)
// - Body lines are executed via a callback so that nesting works automatically

namespace LexorInterpreter.ProgramCodes
{
    public static class IfExecutor
    {
        // Evaluates and executes an IfBlock using the interpreter callback for nested IFs.
        public static string? Execute(
            IfBlock block,
            Dictionary<string, Variable> symbolTable,
            Func<List<(int LineNumber, string Content)>, string?> executeLines)
        {
            foreach (var branch in block.Branches)
            {
                // ELSE branch runs if reached.
                if (branch.Condition == null)
                    return executeLines(branch.Body);

                // Evaluate the bool condition.
                var (value, type, error) = ExpressionEvaluator.Evaluate(
                    branch.Condition,
                    branch.ConditionLine,
                    symbolTable);

                if (error != null) return error;

                if (type != DataType.BOOL)
                    return $"Line {branch.ConditionLine}: IF condition must evaluate to BOOL, got {type}.";

                if ((bool)value!)
                    return executeLines(branch.Body);
            }

            // No branch matched and there is no ELSE; do nothing.
            return null;
        }
    }
}
